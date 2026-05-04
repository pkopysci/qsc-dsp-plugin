# Connection Manager — Spec Delta (M7)

## ADDED Requirements

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
