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

On transition into `Connected`, the ConnectionManager SHALL run a registered list of `IPostConnectAction` hooks in order. M3 populates the list with two actions: `LogonAction` (issues `Logon` with the configured username and password when both are non-empty; skips otherwise) and `HydrateChangeGroupAction` (registers every level-tag and mute-tag from `AudioChannelRegistry` into the `qsc-plugin-state` change group and issues `ChangeGroup.AutoPoll` at 250 ms). The actions SHALL run sequentially: `HydrateChangeGroupAction` MUST wait on the `LogonAction` response (when the action ran) before issuing its first `ChangeGroup.AddControl`. If `LogonAction` returns an error response (code 10 or other), the manager SHALL log `Logger.Warn` and proceed to attempt `HydrateChangeGroupAction`; the Core's response will determine whether subsequent commands are accepted.

#### Scenario: Connect with credentials runs Logon then HydrateChangeGroup in order

- **GIVEN** Initialize was called with non-empty username and password and four channels are registered
- **WHEN** Connect succeeds
- **THEN** the plugin enqueues `Logon` first
- **WHEN** the Logon response arrives successfully
- **THEN** the plugin enqueues four `ChangeGroup.AddControl` requests followed by one `ChangeGroup.AutoPoll`

#### Scenario: Connect without credentials skips Logon

- **GIVEN** Initialize was called with empty username and password
- **WHEN** Connect succeeds
- **THEN** the first request enqueued is the first `ChangeGroup.AddControl` (Logon is skipped)

### Requirement: Disconnect tears down the change group before joining threads

On transition into `Disconnecting`, the ConnectionManager SHALL synchronously issue `ChangeGroup.Destroy` for the active change group before signalling the send and timer threads to stop. This SHALL run in addition to the `CommandQueue.Drain()` and three-thread join behaviour from M2.

#### Scenario: Disconnect order is destroy then drain then join

- **GIVEN** a plugin in Connected
- **WHEN** Disconnect() is called and returns
- **THEN** the plugin sent `ChangeGroup.Destroy` first
- **AND** then drained the command queue
- **AND** then joined the three plugin threads

### Requirement: Single-Core deployments retain unchanged M5 behaviour

When `SetBackupDeviceConnection` was NOT called, the plugin SHALL construct the M5 single-`ConnectionManager` stack on `Connect()` exactly as it did before M6. The redundant-pair code path MUST NOT be exercised in any way; service tier consumers (M3-M5 cache services) MUST continue to enqueue against a plain `CommandQueue`, not the `RoutingCommandQueue` facade.

#### Scenario: Single-Core Connect produces no backup overhead

- **GIVEN** `Initialize` ran but `SetBackupDeviceConnection` was not called
- **WHEN** `Connect()` is called
- **THEN** `BackupDeviceExists` returns `false`
- **AND** the M5 unit-test surface (e.g. `ThreadCensus.Snapshot()` containing exactly `session`, `send`, `keepalive`) is unchanged

### Requirement: Mid-session control add subscribes the new control

When the framework calls `AddInputChannel`, `AddOutputChannel`, `AddPreset`, `AddAudioZoneEnable`, or `AddDspLogicTrigger` while the plugin is in `ConnectionState.Connected`, the implementation SHALL register the control with the in-process registry AND enqueue a `ChangeGroup.AddComponentControl` write naming the new control's QRC tag, followed by a one-shot `ChangeGroup.Poll` to seed the cache. The two QRC writes MUST land on the active `CommandQueue` (the routing facade in redundant deployments). Failure of either write SHALL log `Logger.Warn` and leave the registry registration in place.

When the plugin is not in `Connected`, the registry add behaves as today: stage the control, applied during the next post-connect hydration.

#### Scenario: AddInputChannel mid-session subscribes new control on the wire

- **GIVEN** the plugin is Connected with hydration complete
- **WHEN** `AddInputChannel("in42", "Input.42.gain")` is called
- **THEN** the next two outbound frames on the wire are `ChangeGroup.AddComponentControl` (with `Input.42.gain`) and `ChangeGroup.Poll`

### Requirement: ChangeGroup.Destroy is attempted on graceful disconnect

When `ConnectionManager` enters `Disconnecting` and `_transport.IsConnected` is `true` AND a change group has been hydrated, the implementation SHALL enqueue a single `ChangeGroup.Destroy` write naming the active group id before draining the send queue. Failure of the write (queue refused, transport torn down mid-write, exception) MUST log `Logger.Warn` and the disconnect MUST proceed without further retries.

#### Scenario: Disconnect from Connected attempts Destroy

- **GIVEN** the plugin is Connected with a hydrated change group
- **WHEN** `Disconnect()` is called
- **THEN** a `ChangeGroup.Destroy` frame is enqueued before the transport is closed

#### Scenario: Disconnect from a torn-down transport does not enqueue Destroy

- **GIVEN** the plugin is in `Disconnecting` because the transport already dropped
- **WHEN** the cleanup runs
- **THEN** no `ChangeGroup.Destroy` write is attempted
- **AND** disconnect completes without warnings about the missing Destroy

