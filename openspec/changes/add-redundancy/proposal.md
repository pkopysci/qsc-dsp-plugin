# Change: Add redundancy / failover support

## Why

`IRedundancySupport` is the last DSP-feature interface remaining
before the M7 polish. The framework wants a plugin that can connect
to two physical Q-SYS Cores (primary + backup), automatically
failover writes to whichever Core is currently `Active`, and switch
back to the original primary when it recovers.

QSC's redundancy model (synthesized from `research/QRC_PROTOCOL.md`
§9 and the QSC App Note "Tracking redundant Core usage from a 3rd-
party control system"):

- Both Cores run the same design (`DesignCode` matches), one
  configured `IsRedundant: true`.
- At any moment exactly one is `Active`; the other is `Standby`.
- Each Core pushes an `EngineStatus` notification on TCP connect AND
  whenever its state transitions. The push carries
  `{ State: "Active" | "Standby" | "Idle", DesignName, DesignCode,
  IsRedundant, ... }`.
- Standby Cores reject privileged commands with error `-32604`
  ("Core on Standby"). The plugin must therefore route writes to
  whichever connection the `EngineStatus` push most recently named
  `Active`.

The plugin's job for M6:

1. Maintain a TCP connection to BOTH the primary and the backup
   (after `SetBackupDeviceConnection` lands the backup's hostname).
2. Track each connection's most recently observed `EngineStatus.State`.
3. Route writes to the currently-active connection's command queue.
4. On primary going to `Standby`/`Idle` or dropping the socket,
   switch to the backup.
5. On primary returning to `Active`, switch back per the README's
   literal "switch back" requirement (deviation D3 documents that
   QSC's official guidance is the opposite, but the README wins;
   we expose `RespectQscFailoverGuidance` for operators who prefer
   the QSC-recommended sticky-on-backup behaviour, default `false`).
6. Surface state changes via the `IRedundancySupport` events
   (`RedundancyStateChanged` on switch; `BackupDeviceConnectionChanged`
   on backup TCP up/down).
7. Re-Logon and re-hydrate change groups on the newly-active
   connection (a different physical Core needs its own Logon and
   AutoPoll subscription).

## What Changes

- **New capability spec `redundancy`.** Defines the
  `IRedundancySupport` surface (4 properties + 1 method + 2 events),
  the EngineStatus-driven active-core tracking, the switchback
  policy, and the re-Logon-and-re-hydrate behaviour on switchover.

- **Modified capability spec `connection-manager`.** A
  `RedundantConnectionPair` becomes the new "session" abstraction
  exposed to `QscDspTcp`. It owns two `ConnectionManager` instances
  (primary + backup). Each underlying manager keeps its M2 / M3 /
  M4 / M5 shape unchanged; the pair adds the active-router and the
  EngineStatus subscriber.

- **Source code (under `src/QscDspDevices/`):**
  - `Connectivity/Redundancy/RedundantConnectionPair.cs` — owns
    the two `ConnectionManager` instances. `Connect()` starts both;
    `Disconnect()` stops both. Tracks per-connection
    `EngineStatus.State` via the dispatcher's
    `NotificationReceived` event. Picks an `Active` connection;
    fires `RedundancyStateChanged` when the active changes.
    Implements the 4 `IRedundancySupport` properties as observable
    state.
  - `Connectivity/Redundancy/RoutingCommandQueue.cs` — facade with
    the same `TryEnqueue` API as M2's `CommandQueue`. Forwards
    every enqueue to whichever underlying queue is currently
    associated with the active connection. While neither
    connection is active (initial pre-Connect, both disconnected,
    or both Standby), `TryEnqueue` returns `false` and logs
    `Logger.Error` (matches the M2 "queue refuses while
    disconnected" contract).
  - `Connectivity/Redundancy/EngineStatusObserver.cs` — listens
    on a single `JsonRpcDispatcher.NotificationReceived` for the
    `EngineStatus` method, parses `result.State`, and forwards
    `(connectionId, state)` to the pair coordinator.
  - `Connectivity/Redundancy/SwitchbackPolicy.cs` — single
    boolean `RespectQscFailoverGuidance`. When false (default per
    README), the active flips back to primary on its next
    `Active` push. When true, the active stays on whichever
    connection currently reports `Active`; ties (both `Active`
    transiently during failover) are broken by "current".
  - Modify `Plugin/QscDspTcp.cs` to construct the pair instead of
    the single manager when `SetBackupDeviceConnection` has been
    called; otherwise keep the M2-M5 single-manager path
    unchanged. The 4 `IRedundancySupport` properties expose the
    pair's observed state. The `BackupDevice*` events get raised
    from the pair.

- **Tests:**
  - Unit (xUnit + Moq): `EngineStatusObserverTests`,
    `SwitchbackPolicyTests`, `RoutingCommandQueueTests`,
    `RedundantConnectionPairTests` (focused state-machine tests
    using two `StubTransport` instances).
  - Integration (xUnit + two `FakeQrcServer` instances):
    `Failover_from_primary_to_backup_when_primary_pushes_Standby`,
    `Switchback_to_primary_when_it_returns_to_Active`,
    `BackupDeviceConnectionChanged_fires_on_backup_TCP_up_and_down`,
    `Writes_during_double_Standby_window_are_refused_with_log`.

- **Documentation:**
  - Update `ARCHITECTURE.md` with the redundant-pair state
    machine and the failover sequence diagram.
  - Update `SPEC_COMPLIANCE.md`: discharge rows 5.5
    (redundancy + switchback), 7.2 (lost-connection switch-to-
    backup), and the two `IRedundancySupport` event rows.

## Scope-edge decisions (called out so the critic doesn't have to hunt)

1. **Single backup only.** The framework's surface
   (`SetBackupDeviceConnection` takes one hostname/port pair)
   already constrains this. N-way redundancy is out of scope and
   not on any roadmap.
2. **Active = `EngineStatus.State == "Active"`.** Period. We do
   not heuristically pick an active based on socket health alone;
   if both sockets are up and both Cores report `Standby` (a
   transient between failovers, or a misconfiguration), writes are
   refused. The framework sees this as "not online" via the
   M2 `IsOnline` flag flipping false until one Core re-reports
   Active.
3. **Switchback is on by default per README.** Deviation D3
   already documents this. The `SwitchbackPolicy` flag is the
   escape hatch; it ships defaulted to "respect README" so the
   bet's reviewer sees the contract honoured.
4. **Re-Logon and re-hydrate run synchronously on switchover.**
   Failover triggers the existing post-connect chain on the
   newly-active manager (Logon if creds configured, then change-
   group hydrate). Writes to the active queue resume only after
   hydrate completes; until then `RoutingCommandQueue` continues
   to refuse + log.
5. **`PrimaryDeviceActive` and `BackupDeviceActive` are mutually
   exclusive.** They can both be `false` (no Core active), but
   never both `true`. `BackupDeviceExists` is `true` from the
   moment `SetBackupDeviceConnection` returns successfully (per
   the framework spec), regardless of whether the backup is
   currently online.
6. **Caches are NOT replicated across the pair.** The M3 / M4 /
   M5 caches (`AudioControlService`, `AudioRoutingService`, etc.)
   are single-instance and shared; the pair coordinates which
   underlying connection drives AutoPoll dispatch into them. On
   switchover the new active connection's first AutoPoll cycle
   reconciles the cache with the newly-active Core's actual state
   (which should match the prior active Core, both running the
   same design, but reconnect-style reconciliation handles any
   drift).
