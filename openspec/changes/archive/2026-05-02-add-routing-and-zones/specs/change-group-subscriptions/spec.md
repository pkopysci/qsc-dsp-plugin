# Change Group Subscriptions — Spec Delta

## MODIFIED Requirements

### Requirement: Plugin uses a single AutoPoll change group named qsc-plugin-state

The plugin SHALL maintain at most one QRC change group per connection, named `qsc-plugin-state`, configured for AutoPoll at 250 ms (4 Hz). The hydration action SHALL subscribe every level-tag, mute-tag, output `routerTag` (when non-empty), and zone-enable `controlTag` registered via the audio + zone registries to this group via `ChangeGroup.AddControl`. The QRC protocol limits a connection to four change groups; the plugin's strategy reserves three slots for higher-milestone use.

#### Scenario: Hydration subscribes every M3+M4 control on a typical config

- **GIVEN** four input channels with level/mute tags, four output channels with level/mute/router tags, and 16 zone-enable rows
- **WHEN** Connect succeeds and the post-connect hydration runs
- **THEN** the plugin sends `ChangeGroup.AddControl` for `4×2 + 4×2 + 4 + 16 = 40` distinct control names — all to `Id = "qsc-plugin-state"`
- **AND** the plugin sends one `ChangeGroup.AutoPoll` with `Id = "qsc-plugin-state"` and `Rate = 0.25`
- **AND** `ChangeGroupManager.GroupCount` is `1`
