# Design — Add QRC Client, Connection Manager, and 3-Thread Runtime

## Context

We are building the runtime spine that every later milestone (audio
control, routing, presets, logic, redundancy, ECP) hangs off. The plugin
talks JSON-RPC 2.0 over a `\x00`-delimited TCP stream to a Q-SYS Core
on port 1710 (plaintext only — see deviation D1). It must do this within
three hard constraints from the README:

1. **At most 3 internal threads.**
2. **No public `async`/`await`** — the framework calls our methods
   synchronously and expects them to return promptly.
3. **`gcu_common_utils.NetComs.BasicTcpClient`** is the only sanctioned
   TCP client (see §4 of the README and the explicit citation in
   `openspec/project.md` constraints).

The plugin will be loaded into a long-running framework process on a
Crestron RMC4. A misbehaving plugin must not crash the host
(`Logger.Error` and continue), must not leak threads or sockets across
`Connect`/`Disconnect` cycles, and must surface every state change to
the framework via `BaseDevice.IsOnline` + `NotifyOnlineStatus()`.

## Goals / Non-Goals

**Goals**
- A QRC client that can connect, send commands, receive responses, and
  raise typed events for the rest of the plugin to consume — measurable
  end-to-end against an in-process FakeQrcServer.
- A connection lifecycle that exactly matches the README's literal
  reconnect requirement (15-second wait between attempts, repeats until
  external `Disconnect`) and is verified by deterministic-clock tests.
- A FIFO command queue that refuses-and-logs while disconnected and
  clears-and-logs on every disconnect transition.
- A 3-thread budget enforced by a runtime guard (`ThreadCensus`) so
  any future regression is caught immediately.
- A FakeQrcServer that's spec-faithful enough that swapping in a real
  Q-SYS Core requires no plugin change. Lives in TestSupport, not
  shipped.

**Non-Goals**
- Implementing any audio / routing / preset / logic / redundancy / ECP
  feature — those are later milestones.
- Wrapping `BasicTcpClient` in a feature-rich abstraction. We
  expose only what the protocol layer needs (events, byte-oriented
  send/receive). The transport stays thin and replaceable.
- Performance tuning beyond "the QRC client comfortably handles the
  hundreds-of-messages-per-second AutoPoll updates the spec describes".
  Microbenchmarks come in M7.

## Decisions

### Decision: Three threads, fixed roles, owned by `QscDspTcp`

| Thread | Role | Owner |
|--------|------|-------|
| **T1 send-loop**    | Drains `CommandQueue`, writes JSON-RPC frames to the active transport | `Protocol/SendLoop.cs` |
| **T2 receive-loop** | Reads bytes from the transport, splits on `\x00`, deserializes, routes to dispatcher | `Protocol/ReceiveLoop.cs` |
| **T3 timer**        | Drives `KeepaliveTimer` (NoOp every 30s), `ReconnectTimer` (15s), and any future periodic | `Plugin/Threading/PluginTimer.cs` |

Threads are created in `ConnectionManager.OpenSession()` and joined in
`CloseSession()`. Lifetime is **per-session**, not per-plugin: a
disconnect joins all three; a subsequent reconnect spins up new ones.
This means a long-running plugin that never disconnects has exactly 3
threads ever; a plugin that reconnects N times still has 3 threads at
any given moment.

`ThreadCensus` (`Plugin/Threading/ThreadCensus.cs`) registers each
thread on start and unregisters on exit. Any breach (a fourth
registration while three are alive) trips a `Logger.Error`, sets a
breach flag, and (in DEBUG builds only) calls `Environment.FailFast`
so it surfaces in tests immediately. RELEASE builds log and continue
to honour "must not crash the host."

**Why per-session lifetimes vs persistent threads.** A persistent
send-loop has to handle "transport is null" / "transport just changed"
state on every iteration. A per-session loop owns its transport from
start to join. Disposal is simpler; thread-leak bugs become impossible
because joining three threads is straightforward in `CloseSession`.

### Decision: Internal `Task`/`async` for plumbing; sync public surface

The README forbids public `async`/`await`. Internally we use private
`async Task` methods because they make the receive-loop and timer
straightforward to write correctly. The public methods on `QscDspTcp`
that the framework calls (`Connect`, `Disconnect`,
`SetAudioOutputLevel`, etc.) all return `void` synchronously — they
**enqueue** and immediately return. The framework never sees a `Task`.

This also avoids the SynchronizationContext / deadlock pitfalls that
bite plugins running inside the Crestron host's main loop.

### Decision: One framer per connection, bounded in/out buffers

The framer (`Protocol/QrcFramer.cs`) owns a growable `ArrayBufferWriter<byte>`
per connection. After each `transport.Receive()`, it scans for `\x00`,
emits each completed frame, and keeps the partial tail. Initial buffer
16 KiB; doubles up to a configurable max (default 16 MiB). Past the
max we log `Logger.Error` "frame too large" and force a reconnect —
this prevents a malicious or buggy peer from causing unbounded
allocation.

Outbound writes are direct: build the JSON, encode UTF-8, append `\x00`,
hand to transport. No outbound buffering — the queue handles
back-pressure.

### Decision: Id-correlation + AutoPoll subscription map for dispatch

The dispatcher (`Protocol/JsonRpcDispatcher.cs`) maintains:

- `ConcurrentDictionary<long, TaskCompletionSource<JsonElement>> _pending` —
  outstanding requests by their monotonic int64 id.
- `ConcurrentDictionary<long, IAutoPollSubscription> _autoPolls` —
  subscriptions registered by `ChangeGroup.AutoPoll` whose pushed
  responses share the original request's id (a well-known QRC quirk
  documented in `research/QRC_PROTOCOL.md` §13.4).
- `_notifications` event — fires for `method`-bearing messages with no
  `id` (`EngineStatus`, server-driven `LoopPlayer.Error`, etc.).

When a frame arrives with both `id` and `result`, dispatcher checks
`_autoPolls` first; if hit, push to subscription. Else complete the
matching `_pending` TCS. Else log `Logger.Warn "unknown id <n>"`.

### Decision: Command queue is a `Channel<QrcRequest>` with a status flag

`Protocol/CommandQueue.cs` is built on `System.Threading.Channels.Channel<T>`
because it's the cleanest single-writer-many-readers FIFO in .NET
without needing locks. A single sentinel field `_acceptingCommands`
gates `TryEnqueue` — when false, every call returns false and logs
`Logger.Error` "command attempted while disconnected". On every
disconnect transition the connection manager calls `Drain()` which
flips `_acceptingCommands` false and dequeues + discards everything.

Bound: 1024 outstanding requests. If reached, log `Logger.Warn
"command queue saturated"` and drop the oldest (mirror's the policy
of "newer commands should win when the device can't keep up").

### Decision: Connection manager is a hand-rolled state machine

```
                ┌─────────────┐
                │ Disconnected│◄─────────────┐
                └──────┬──────┘              │
                       │ Connect()           │ Disconnect() / fault
                       ▼                     │
                ┌─────────────┐              │
                │ Connecting  │──────────────┤
                └──────┬──────┘     fail     │
                       │ success             │
                       ▼                     │
                ┌─────────────┐              │
                │  Connected  │──────────────┤
                └──────┬──────┘  Disconnect()│
                       │ Disconnect()        │
                       ▼                     │
                ┌─────────────┐              │
                │Disconnecting│──────────────┘
                └─────────────┘
```

Every transition is logged at `Notice`. `IsOnline` is set BEFORE
`NotifyOnlineStatus()` is called (per README §3 explicit ordering
requirement). On `Connecting → Connected`, the manager enqueues the
hydration sequence (empty until M3 fills it). On any transition into
`Disconnected`, the queue is drained and the three session threads
are joined.

**Reconnect.** When a connection drops unexpectedly (transport
fault) and the user has not called `Disconnect()`, the manager
schedules a fresh connection attempt 15 seconds in the future via
the timer thread. If that attempt also fails, another 15 seconds.
Loop continues until either success or `Disconnect()` is called.
This mirrors the README's "wait 15 seconds, and attempt to reconnect.
This will be repeated until `BaseDevice.Disconnect()` is called
externally or a connection is established."

The 15-second interval is **constant**, not exponential backoff — the
README is specific.

### Decision: Deterministic clock for test timing

All time-aware code (`KeepaliveTimer`, `ReconnectStrategy`) takes an
`IQrcClock` constructor parameter. Production wires
`Plugin/Threading/SystemClock.cs` (`DateTime.UtcNow`,
`Task.Delay(TimeSpan)`). Tests wire
`tests/.../TestSupport/DeterministicClock.cs` (manually-advanced
virtual time, `WaitFor(TimeSpan)` returns immediately).

This is what lets us prove the 15-second reconnect *exactly* without
sleeping in CI.

### Decision: Fake QRC server lives in TestSupport, not Integration tests

Because all three test projects (Unit, Integration, Property) need it,
the FakeQrcServer goes in `QscDspDevices.TestSupport` so each test
project gets it via `<ProjectReference>`. Failure-injection knobs are
public methods (`DropConnection`, `DelayResponseMs`, `EmitMalformed`,
`RespondWithStandbyError`).

### Decision: No public new types added to QscDspTcp's contract

This milestone introduces the *runtime* (transport, framer, dispatcher,
queue, connection manager). The framework-facing surface is unchanged
from M1: `QscDspTcp` only adds the BaseDevice/IDsp overrides that
M1 already named in `SPEC_COMPLIANCE.md`. M3+ will add the
audio/routing/preset/etc. method bodies but not new types on
`QscDspTcp` either — those interfaces all live on framework types
the plugin already implements.

## Risks / Trade-offs

- **`BasicTcpClient` is stubbed (NotImplementedException) in
  FrameworkStubs.** Production code will run against the real DLL at
  delivery time. Tests use `FakeQrcServer` connecting via
  `BasicTcpClient` only at the seam — and the seam is mocked in unit
  tests via `IConnectionTransport`. Integration tests skip the real
  `BasicTcpClient` by using a direct `TcpClient`-based transport
  variant we ship inside TestSupport. This means we never invoke
  the stubbed surface during tests, so NotImplementedException can't
  bite us. **Mitigation:** an explicit unit test asserts that the
  production-only `BasicTcpClientTransport` constructor wires its
  callbacks correctly (using a Moq-mocked BasicTcpClient).
- **The QRC protocol's "AutoPoll responses share the request id" quirk
  is easy to miss in dispatch.** Mitigation: a dedicated unit test
  `JsonRpcDispatcher_AutoPoll_routes_pushed_responses_to_subscription`.
- **Thread census is best-effort — a thread spawned via a third-party
  library (e.g. inside Crestron SDK itself) bypasses it.** Mitigation:
  the census is a development guard against OUR own regressions;
  framework-internal threads are out of our control and outside the
  README's intent.
- **`Channel<T>` introduces a dependency on
  `System.Threading.Channels`.** It ships in the .NET runtime so no
  extra NuGet. DLL size impact is zero.
- **Reconnect storm risk.** If the Core's port is reachable but
  rejecting at the application layer, we'd reconnect every 15s
  forever. Mitigation: log every attempt at `Logger.Notice`; the
  framework operator can `Disconnect()` to stop. Per README, the
  loop continues "until `Disconnect()` is called externally" —
  this is by spec, not a bug.

## Migration Plan

Greenfield additions; no existing behaviour to migrate. The merged M1
DLL is empty so consumers (none yet, AV Framework will be the first)
gain new functionality without breaking anything.

## Open Questions

- **Outbound write buffering on send-loop.** Currently the send-loop
  takes one item from the channel, serializes, writes, repeats. For
  small commands this is fine; if the framework calls
  `SetAudioOutputLevel` 100 times in a row we'd issue 100 writes. We
  can batch in M3 if benchmarking shows it matters; M2 keeps it simple.
- **Logging payload sensitivity.** `Logger.Debug` could log raw QRC
  frames (helpful) but might include `Logon` PINs. M2 will redact
  `Logon` payloads explicitly in the framer's debug-log path. Will
  document in the qsc-critic review that this redaction is honored.
