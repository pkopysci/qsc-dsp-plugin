# Architecture — QscDspDevices

> **Audience.** Reviewers, future maintainers, and anyone who has to
> reason about the plugin's threading, queueing, and failover. The README
> is the contract; this document is the implementation map. The spec
> compliance matrix (`SPEC_COMPLIANCE.md`) is the audit trail.

This document is filled out as milestones land. Empty subsections list
the milestone that introduces them.

## Layers

```
┌──────────────────────────────────────────────────────────────┐
│  Plugin                                                      │
│  - QscDspTcp (the public root class implementing the         │
│    framework's IDsp / IAudioRoutable / IAudioZoneEnabler /   │
│    IDspLogicTriggerSupport / IRedundancySupport interfaces)  │
└──────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌──────────────────────────────────────────────────────────────┐
│  Connectivity                                                │
│  - ConnectionManager  (state machine: Disconnected →         │
│    Connecting → Connected; reconnect loop @ 15s)             │
│  - RedundantCorePair  (primary + backup; switchback)         │
│  - StateHydrator      (post-connect: rehydrate registry)     │
└──────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌──────────────────────────────────────────────────────────────┐
│  Backends                                                    │
│  - QrcBackend         (primary; full feature set)            │
│  - EcpBackend         (fallback; capability-limited)         │
└──────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌──────────────────────────────────────────────────────────────┐
│  Protocol                                                    │
│  - JsonRpcFramer      (null-byte framing; UTF-8)             │
│  - JsonRpcDispatcher  (id-correlation, AutoPoll subscription │
│    map, server-notification routing)                         │
│  - CommandQueue       (FIFO; cleared on disconnect)          │
│  - ChangeGroupManager (≤ 4 groups; reserves 3 for plugin)    │
│  - EcpFramer / EcpDispatcher (ASCII line framing)            │
└──────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌──────────────────────────────────────────────────────────────┐
│  Transport                                                   │
│  - IConnectionTransport                                      │
│  - BasicTcpClientTransport (uses gcu_common_utils.NetComs    │
│    .BasicTcpClient — required by README §4)                  │
└──────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌──────────────────────────────────────────────────────────────┐
│  Domain                                                      │
│  - InputChannel / OutputChannel / Preset / LogicTrigger /    │
│    ZoneEnableToggle (state mirror; raises events AFTER       │
│    update, never before)                                     │
│  - GainScaler (0–100 ↔ device-native; injective)             │
│  - MatrixRouter                                              │
└──────────────────────────────────────────────────────────────┘
```

## Threading model — three-thread cap, evolved across milestones

The README §4 caps the plugin's internal threads at three. Each
milestone tightens the layout of those threads as the work that needs
threading is implemented.

### M2 (shipped)

M2's only long-running plugin work is the connection lifecycle. The
plugin spins up exactly **one** background `Task` per session, scheduled
on the .NET thread-pool:

| Component | Where it runs | Lifetime |
|-----------|---------------|----------|
| `ConnectionManager.RunSessionAsync` | thread-pool task started by `Connect()` | one per session (joined on `Disconnect()` / `Dispose()`) |
| `ReconnectStrategy.WaitForNextAttemptAsync` | inline within the session task | until success or cancellation |
| `KeepaliveTimer.TickAsync` | not yet wired to any external pump in M2; standalone, exercised only by tests | n/a until M3 |
| Public method bodies (`QscDspTcp.Connect`/`Disconnect`/M3-stub mutators) | framework calling thread | synchronous |

`ThreadCensus` (`Plugin/Threading/ThreadCensus.cs`) is shipped and unit-
tested but is also wired into the session task, so any production code
path that spawns a plugin-owned thread participates in the budget. The
guard logs `Logger.Error` "thread budget breached" and (in DEBUG only)
calls `Environment.FailFast` if a 4th thread registers while three are
alive.

### M3 (shipped)

M3 lit up the steady-state work pattern (active change-group AutoPoll,
audio control, snapshot recall, Logon) and the I/O loops the queue and
dispatcher feed into. The threading shape is a more pragmatic variant
of the originally-planned canonical layout below: instead of three
dedicated `Thread` instances, the manager spawns two threadpool
`Task`s — a send loop and a keepalive ticker — alongside an
event-driven receive path on the transport's `RxReceived` event. The
overall budget is still ≤ 3 plugin-owned units of work, all registered
with `ThreadCensus`.

| Unit of work | Purpose | Lifetime | Owner |
|--------|---------|----------|-------|
| Send task | `Task.Run` draining `CommandQueue.DequeueAsync`, JSON-serializes + frame-encodes, writes to `IConnectionTransport.Send`. Notifies keepalive on every successful write. | per session | `ConnectionManager.RunSendLoopAsync` |
| Receive event handler | `transport.RxReceived` → `QrcFramer.Append` (carries split-frame state across reads) → `JsonRpcDispatcher.Dispatch`. Runs on whatever threadpool worker the transport raises the event on. | per session | `ConnectionManager.StartIoLoops` (anonymous handler) |
| Keepalive task | `Task.Run` ticking `KeepaliveTimer.TickAsync` once per second; the timer itself only emits a `NoOp` after 30 s of outbound silence. | per session | `ConnectionManager.RunKeepaliveLoopAsync` |

The dedicated `Thread` instances called out in the original M3 plan
were dropped in favour of `Task`-based equivalents because (a) the
README's budget rule constrains *count*, not *type*, of plugin-owned
work, (b) `Task.Run` fits the rest of the codebase (M2 session
loop, `KeepaliveTimer.TickAsync`) without inventing a new lifecycle
manager, and (c) `ThreadCensus`'s token-based registration gives
us the budget guarantee regardless of underlying thread reuse.

If a future milestone (M5/M6) finds it actually needs OS-thread
pinning — for hard-realtime keepalive on a busy threadpool, say — the
`PluginThreads.cs` shim can be reintroduced without changing the
public contract of `ConnectionManager`.

## Post-connect hydration sequence — M3 (shipped)

Each (re)connect runs a `CompositePostConnectAction` of two steps:

1. **`LogonAction`** — issues `Logon { User, Password }` if either
   credential is non-empty; awaits the response with a 5 s timeout.
   Empty creds skip; error response logs warn + continues.
2. **`HydrateChangeGroupAction`** — awaits Logon completion (when
   present), then for every channel registered via
   `AudioChannelRegistry`, enqueues `ChangeGroup.AddControl` for the
   level-tag, mute-tag, and (M4) the output's `routerTag` when
   non-empty. Also (M4) enqueues `AddControl` for every
   `(channelId, zoneId)` row in `AudioZoneRegistry`, and (M5) for
   every registered logic-trigger tag in `LogicTriggerRegistry`.
   Then registers the AutoPoll id with the dispatcher and enqueues
   `ChangeGroup.AutoPoll` at 250 ms. The single `qsc-plugin-state`
   group still sits well under the QRC 4-group cap.

The Core's AutoPoll responses then drive `AudioControlService` cache
updates and the `IAudioControl` event surface.

## Key state machines

> Filled out as milestones land. Each diagram cites the spec compliance
> row it discharges.

### Connection state — M2 (shipped)

```
              ┌────────────────────┐
              │   Disconnected     │◄────────────────┐
              └──────────┬─────────┘                 │
                         │ Connect()                 │ user Disconnect()
                         ▼                           │ OR transport fault
              ┌────────────────────┐                 │
              │     Connecting     │─────────────────┤
              └──────────┬─────────┘  attempt fails  │
                         │ transport.Connected event │
                         ▼                           │
              ┌────────────────────┐                 │
              │     Connected      │─────────────────┘
              │ (queue accepting,  │
              │  postConnect ran)  │  Disconnecting is a
              └──────────┬─────────┘  brief transient state
                         │ user Disconnect()
                         ▼
              ┌────────────────────┐
              │   Disconnecting    │
              └──────────┬─────────┘
                         │ session task joined
                         └────────────► Disconnected
```

The state machine is implemented in `src/QscDspDevices/Connectivity/ConnectionManager.cs`.
Every transition is logged at `Logger.Notice` with the from-state, to-state, and
cause. After a transport-reported fault, the manager transitions immediately
to `Disconnected` (NOT `Connecting` 15 s later) so observers see the offline
state without delay — discovered by integration test
`Server_drop_triggers_state_back_into_Connecting_on_reconnect` and fixed in
commit `a2f2117`. The 15-second wait between attempts is a separate
`ReconnectStrategy` consulted on the next loop iteration.

`QscDspTcp.OnStateChanged` translates these transitions to
`BaseDevice.IsOnline` + `BaseDevice.NotifyOnlineStatus()` — IsOnline is set
BEFORE NotifyOnlineStatus per README §3 (verified by test
`Connect_drives_IsOnline_true_then_NotifyOnlineStatus` reading IsOnline
inside the ConnectionChanged handler).

### Redundant core failover — M6

(_to be added in M6_)

## Concurrency policy

- **No `lock` held across an `event` raise.** Events fire after the
  caller drops the relevant lock. Otherwise a subscriber that calls back
  into the plugin would re-enter and deadlock.
- **No `lock` held across a network I/O call.** The send loop holds no
  domain-state lock while writing.
- **Lock order.** Documented per-class with a `// Lock order:` comment
  in the class header. Global order: `connection > queue > registry >
  channel`. A stricter lock taken before a looser one is the only
  permitted direction.
- **Cancellation tokens.** Every internal worker accepts a
  `CancellationToken` plumbed from the plugin's lifetime token. `Disconnect()`
  cancels and joins; `Dispose()` cancels, joins, then cleans transport
  resources.
- **Volatile / Interlocked.** Hot paths (`IsOnline`, `IsInitialized`)
  use `Volatile.Read`/`Volatile.Write` rather than `lock` to avoid
  caller contention.

## Error model

Every public mutator follows the same shape:

```csharp
public void SetAudioOutputLevel(string id, int level)
{
    try
    {
        // 1. Argument validation via gcu_common_utils.Validation.ParameterValidator
        // 2. Look up channel (return + log Warn if missing)
        // 3. Enqueue command (return + log Error if disconnected)
    }
    catch (Exception ex)
    {
        // Never let an exception cross the plugin boundary.
        Logger.Error(LogServiceTypes.Hardware, LogDeviceTypes.Dsp, Id,
                     $"{nameof(SetAudioOutputLevel)} failed: {ex.Message}");
    }
}
```

QRC error codes round-trip to a typed `enum QrcErrorCode`. Every code is
mapped explicitly; an unknown code logs `Logger.Warn` and is treated as
`ServerError`.

## Build, package, and ship layout

- Single shipped DLL: `QscDspDevices.dll` from `src/QscDspDevices/`.
- Stub assembly `FrameworkStubs.dll` is **never shipped**
  (`<IsPackable>false</IsPackable>`).
- Test assemblies are never shipped.
- Crestron SDK, Newtonsoft.Json are runtime-pulled by the framework
  host; the plugin assumes they are already present in the AppDomain
  per the AV Framework's plugin loader contract.

## What we deliberately do NOT do

- **No `Task`/`async` on the public API.** The plugin presents synchronous
  methods; long work is queued and confirmed via events.
- **No DI container internally.** Constructor injection of explicit
  collaborators is used; `Microsoft.Extensions.DependencyInjection` is
  not pulled in (would inflate the DLL).
- **No `System.Reactive`.** Events are plain `EventHandler<T>` for
  framework compatibility.
- **No JSON deserialization without bounds.** Frames over 16 MiB are
  rejected with a fatal log; the read buffer is bounded.
- **No swallowed exceptions.** Every catch logs and either returns a
  documented fallback or rethrows internally to a top-level handler.
