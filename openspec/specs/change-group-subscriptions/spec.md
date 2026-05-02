# change-group-subscriptions Specification

## Purpose
TBD - created by archiving change add-audio-control-and-presets. Update Purpose after archive.
## Requirements
### Requirement: Plugin uses a single AutoPoll change group named qsc-plugin-state

The plugin SHALL maintain at most one QRC change group per connection, named `qsc-plugin-state`, configured for AutoPoll at 250 ms (4 Hz). The hydration action SHALL subscribe every level-tag, mute-tag, output `routerTag` (when non-empty), zone-enable `controlTag`, and registered logic-trigger `tagName` to this group via `ChangeGroup.AddControl`. The QRC protocol limits a connection to four change groups; the plugin's strategy reserves three slots for higher-milestone use.

#### Scenario: Hydration subscribes every M3+M4+M5 control on a typical config

- **GIVEN** four input channels (level + mute), four output channels (level + mute + router), 16 zone-enable rows, and four logic triggers
- **WHEN** Connect succeeds and the post-connect hydration runs
- **THEN** the plugin sends `ChangeGroup.AddControl` for `4×2 + 4×3 + 16 + 4 = 40` distinct control names — all to `Id = "qsc-plugin-state"`
- **AND** the plugin sends one `ChangeGroup.AutoPoll` with `Id = "qsc-plugin-state"` and `Rate = 0.25`
- **AND** `ChangeGroupManager.GroupCount` is `1`

### Requirement: AutoPoll deltas are routed to AudioControlService

When an AutoPoll response arrives, the change-group manager SHALL parse the `Changes` array and, for each entry, route `(controlName, value)` to a registered callback. In M3 the only registered callback is `AudioControlService.OnDeviceUpdate`.

#### Scenario: AutoPoll response with two deltas dispatches twice

- **GIVEN** the manager has a registered callback
- **WHEN** an AutoPoll response arrives with `Changes: [{ Name: "a", Value: 1 }, { Name: "b", Value: false }]`
- **THEN** the callback is invoked twice in order, once per entry

### Requirement: Disconnect destroys the change group before threads quiesce

On transition into `Disconnecting`, the plugin SHALL synchronously issue `ChangeGroup.Destroy` with `Id = "qsc-plugin-state"` on the receive thread before signalling the send and timer threads to stop. If the destroy times out (deadline 1 s), the plugin SHALL proceed with the disconnect and rely on the Core's eventual group-on-socket-close GC.

#### Scenario: Disconnect issues Destroy then joins threads

- **GIVEN** a plugin in Connected with the change group active
- **WHEN** Disconnect() is called
- **THEN** `ChangeGroup.Destroy` is sent (or its 1 s deadline expires)
- **AND** then the three plugin threads stop and join

### Requirement: Reconnect rebuilds the change group from the registry

After every (re)connect, the post-connect hydration MUST rebuild the change group from the current `AudioChannelRegistry` contents. The rebuild MUST NOT rely on the Core remembering any previous group state.

#### Scenario: After a server-side socket drop and reconnect, the group has the same controls

- **GIVEN** four channels were subscribed before a mid-flight drop
- **WHEN** the connection re-establishes
- **THEN** the plugin issues `ChangeGroup.AddControl` for each of the four tags again
- **AND** issues `ChangeGroup.AutoPoll` with `Rate = 0.25` again

