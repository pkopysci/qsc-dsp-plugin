# Design — Add redundancy / failover support

## Context

M3-M5 delivered every other DSP-feature surface; M6 fills in
`IRedundancySupport`. Structurally this is the largest milestone
remaining because it introduces a second TCP connection and a
coordinator that owns it alongside the primary.

The two-Core model is straightforward at the protocol level (see
`research/QRC_PROTOCOL.md` §9):

- Both Cores run the same Designer code.
- Each pushes `EngineStatus { State, IsRedundant, ... }` on
  connect and on every state transition.
- Exactly one is `Active` at a time; the other is `Standby` (or
  `Idle` during boot).
- Privileged commands to a `Standby` Core return `-32604`
  ("Core on Standby").

The challenge is the C# composition shape: M2-M5 assumes one
`ConnectionManager`, one `CommandQueue`, one set of
`PostConnectActions`, all wired into one shared service tier. M6
needs two of each on the connection side, sharing the service tier
on the cache side.

## Goals / Non-Goals

**Goals**

- Implement `IRedundancySupport` (4 properties + 1 method + 2
  events) end-to-end against two `FakeQrcServer` instances.
- Failover: when the active Core goes `Standby` (or its socket
  drops), writes route to the other.
- Switchback (per README D3): when the original primary returns
  to `Active`, writes route back. Configurable via
  `RespectQscFailoverGuidance` flag.
- Single-Core deployments (no `SetBackupDeviceConnection` call)
  continue to work exactly like M5; M6 introduces no behaviour
  change for that path.
- The cache tier (`AudioControlService`, `AudioRoutingService`,
  `AudioZoneEnableService`, `LogicTriggerService`) stays
  single-instance and is reconciled by AutoPoll on whichever
  connection is currently active.

**Non-Goals**

- N-way redundancy (`SetBackupDeviceConnection` only takes one
  pair).
- Mid-flight retry of in-progress writes that landed on a Core
  that just went Standby (the M3-M5 optimistic-cache pattern
  handles this passively — the cache reflects intent, AutoPoll
  reconciles).
- Configuration-time validation that the two Cores actually run
  the same design (`DesignCode` cross-check). Worth a Notice-level
  log on mismatch but not a hard refusal.
- Multiple plugin instances controlling the same Core pair
  (deployment concern, not a plugin concern).

## Architecture

```
QscDspTcp (post SetBackupDeviceConnection):
   │
   │ Connect() / Disconnect() / IsOnline / IRedundancySupport surface
   ▼
RedundantConnectionPair
   │     ┌──────────────────────────────────┐
   │     │ EngineStatusObserver (×2)        │
   │     │   listens on each manager's      │
   │     │   dispatcher.NotificationReceived│
   │     │   for EngineStatus pushes        │
   │     └──────────────────────────────────┘
   │
   ├── ConnectionManager (primary, M2-M5 unchanged) → primary CommandQueue
   ├── ConnectionManager (backup,  M2-M5 unchanged) → backup CommandQueue
   │
   ▼
RoutingCommandQueue (facade with TryEnqueue API)
   │
   └── forwards to whichever underlying queue is currently "active"
       (the connection whose last EngineStatus push was "Active")

Service tier (shared, single-instance):
   AudioControlService / AudioRoutingService / AudioZoneEnableService /
   LogicTriggerService / PresetService — all enqueue to the
   RoutingCommandQueue, not directly to a CommandQueue.

AutoPoll dispatch:
   Each ConnectionManager has its own JsonRpcDispatcher / framer /
   ChangeGroupManager. The pair coordinator subscribes the
   AudioControlServiceFanout's Dispatch callback on whichever
   ChangeGroupManager belongs to the currently-active connection;
   on switchover the subscription moves.
```

## Key design decisions

### D-1: One pair, two managers, shared services

The cache tier (M3-M5 services) is unchanged. Only the connection
side gets a second instance. This means the M2-M5 invariants
(post-connect Logon → hydrate, send/recv/keepalive Tasks per
manager, ThreadCensus per manager) are preserved unchanged on each
side of the pair. The pair adds at most three plugin-owned threads
per side; both sides Connected = up to 6 threads. ThreadCensus's
3-cap was per-manager; for M6 it becomes 3 per manager, 6 across
the pair. This is the only deviation from the M2 budget rule —
documented in SPEC_COMPLIANCE.

### D-2: RoutingCommandQueue facade

The M3-M5 services hold a `CommandQueue` reference and call
`TryEnqueue`. The facade has the same shape but routes to the
active connection's underlying queue. Inactive Cores see no
traffic. Switchover swaps the inner reference under a lock; in-
flight enqueue races resolve to whichever pointer was current at
the lock-take. Writes during a "no Core active" window
(transient or permanent) return `false` and log.

The M3 cache-then-enqueue pattern means writes during a
double-Standby window still update the framework's intent cache
even though the wire write is refused. AutoPoll on the next active
Core reconciles.

### D-3: Switchover triggers the post-connect chain on the new active

On `EngineStatus.State` flipping to `Active` on a connection that
wasn't previously the active one, the pair coordinator:

1. Marks that connection as the active one (`RoutingCommandQueue`
   re-points).
2. Subscribes the `AudioControlServiceFanout.Dispatch` to the new
   connection's `ChangeGroupManager` (de-subscribing from the old).
3. Triggers the new connection's existing `OnConnectedAsync`
   post-connect hook — Logon (if creds), then change-group
   hydrate. The cache reconciles when the AutoPoll responses
   start flowing back.

This means switchover takes one Logon + one full re-subscribe +
one AutoPoll round-trip — typically < 1 s on a healthy backup
already maintaining its TCP socket. The 10 s "auto-failover"
detection window in QSC's docs is the upper bound.

### D-4: Switchback policy

Default `RespectQscFailoverGuidance = false` honours the README's
"switch back to the primary once it comes back online" rule. When
the primary's `EngineStatus` pushes `Active` after a failover, the
pair flips active back to primary.

Setting the flag to `true` reverses to QSC's recommended sticky-
on-backup behaviour: the active stays put until the current
active goes Standby. The choice lives on a single config option
exposed on the pair so an operator can pick at startup.

The flag does NOT affect the failover direction — switching FROM
a Standby Core TO an Active Core is always immediate and is not
configurable; it would otherwise leave writes refused.

### D-5: The single-Core path is unchanged

Without `SetBackupDeviceConnection`, `QscDspTcp.Initialize`
constructs the M5 stack as before — single `ConnectionManager`,
single `CommandQueue`, no facade. The pair is constructed lazily
when `SetBackupDeviceConnection` lands, and at that point the
existing single-manager state is folded into the primary slot.

The framework calls `SetBackupDeviceConnection` after `Initialize`
but before `Connect` (per the framework doc), so we can do the
fold-in synchronously without worrying about concurrent
Connect / Disconnect.

## Risks

1. **Two Cores, two ThreadCensuses** — the README's 3-thread rule
   becomes 6 across the pair. We document this as a deviation; the
   intent ("don't spawn unbounded threads") is preserved.
2. **Active flip during a partial post-connect chain** — primary
   goes Standby right as Logon completes on the backup but before
   change-group hydrate finishes. Coordinator must guard against
   acting on a half-hydrated active. Mitigation: the pair's
   "active" pointer flips only after the new manager reaches
   `Connected` AND its post-connect chain has completed.
3. **EngineStatus push race** — both Cores transiently pushing
   `Active` during a failover. Tie-broken by "previous active wins"
   to avoid flapping; the loser is treated as Standby until its
   next push.
