# connection-manager Specification

## Purpose
TBD - created by archiving change add-qrc-client-and-connection. Update Purpose after archive.
## Requirements
### Requirement: ConnectionManager owns the session lifecycle as a state machine

The plugin SHALL implement a `ConnectionManager` that exposes states `Disconnected`, `Connecting`, `Connected`, and `Disconnecting`. Every transition MUST log `Logger.Notice` with the from-state, the to-state, and the cause. State changes MUST be serialized so external observers never see an inconsistent snapshot.

#### Scenario: Lifecycle progresses Disconnected -> Connecting -> Connected -> Disconnecting -> Disconnected

- **GIVEN** a ConnectionManager in Disconnected
- **WHEN** Connect() is called and the transport reports success
- **THEN** the state progresses to Connecting then Connected, each logged at Notice
- **WHEN** Disconnect() is then called
- **THEN** the state progresses to Disconnecting then Disconnected, each logged at Notice

### Requirement: IsOnline is set BEFORE NotifyOnlineStatus

On every state transition that changes the device's online status, the plugin MUST set `BaseDevice.IsOnline` to its new value before calling `BaseDevice.NotifyOnlineStatus()`. This ordering is explicit in README §3 and any reversal is a violation.

#### Scenario: A subscriber reading IsOnline inside the NotifyOnlineStatus handler sees the new value

- **GIVEN** an external subscriber attached to ConnectionChanged
- **WHEN** the connection transitions from Connected to Disconnected
- **THEN** by the time the ConnectionChanged event fires, `IsOnline` is already false

### Requirement: Reconnect after unexpected disconnect, exactly 15 seconds apart

When the transport reports a disconnect that was not initiated by `Disconnect()`, the ConnectionManager MUST schedule a reconnect attempt 15 seconds later. If that attempt fails, another attempt MUST be scheduled 15 seconds after the failure. The cycle continues until either `Disconnect()` is called or a connection is established. The interval is a constant 15 seconds; no exponential backoff.

#### Scenario: Three failed attempts, then Disconnect stops the loop

- **GIVEN** the deterministic clock starts at t=0 and the transport will fail every Connect attempt
- **WHEN** an unexpected disconnect occurs at t=0 and the clock advances to t=46s without any other intervention
- **THEN** exactly three reconnect attempts have been made (at t=15, t=30, t=45)
- **WHEN** Disconnect() is then called
- **THEN** no further reconnect attempts are scheduled

### Requirement: Disconnect drains the command queue and joins all session threads

A transition into `Disconnected` MUST call `CommandQueue.Drain()` and MUST join the three per-session threads (send-loop, receive-loop, timer) before the transition is considered complete. After disconnect, no plugin-owned thread SHALL remain alive aside from any future-session threads spawned by a subsequent `Connect()`.

#### Scenario: Thread census reads zero plugin threads after Disconnect

- **GIVEN** a ConnectionManager that completed a Connect cycle and has 3 plugin threads alive
- **WHEN** Disconnect() is called and returns
- **THEN** the ThreadCensus reports zero plugin-owned threads alive

### Requirement: Hydration hook fires after Connected

On transition into `Connected`, the ConnectionManager SHALL invoke a registered `IPostConnectAction.RunAsync()` hook. The hook is empty in M2; later milestones (M3 onwards) populate it to subscribe to ChangeGroups and refresh the registered control state.

#### Scenario: Empty hook completes without error

- **GIVEN** the empty default `IPostConnectAction` is registered
- **WHEN** the ConnectionManager transitions to Connected
- **THEN** the hook is invoked and completes
- **AND** the state remains Connected

