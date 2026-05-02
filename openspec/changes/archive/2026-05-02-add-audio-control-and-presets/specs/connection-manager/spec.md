# Connection Manager — Spec Delta

## MODIFIED Requirements

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

## ADDED Requirements

### Requirement: Disconnect tears down the change group before joining threads

On transition into `Disconnecting`, the ConnectionManager SHALL synchronously issue `ChangeGroup.Destroy` for the active change group before signalling the send and timer threads to stop. This SHALL run in addition to the `CommandQueue.Drain()` and three-thread join behaviour from M2.

#### Scenario: Disconnect order is destroy then drain then join

- **GIVEN** a plugin in Connected
- **WHEN** Disconnect() is called and returns
- **THEN** the plugin sent `ChangeGroup.Destroy` first
- **AND** then drained the command queue
- **AND** then joined the three plugin threads
