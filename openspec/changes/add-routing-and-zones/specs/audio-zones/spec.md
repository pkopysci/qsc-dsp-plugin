# Audio Zones — Spec Delta

## ADDED Requirements

### Requirement: AddAudioZoneEnable registers the controlTag and ignores duplicate pairs

When the framework calls `AddAudioZoneEnable(channelId, zoneId, controlTag)`, the plugin SHALL register the triple in `AudioZoneRegistry`, keyed on `(channelId, zoneId)`. If a row with the same pair already exists, the plugin SHALL ignore the new registration per `framework-docs/gcu-hardware-service/IAudioZoneEnabler.md` ("the new one will be ignored") and log `Logger.Notice` so the drop is observable in diagnostics.

#### Scenario: Second registration for the same pair is dropped

- **GIVEN** the registry already holds `("mic1", "zoneA") → "tagA"`
- **WHEN** the framework calls `AddAudioZoneEnable("mic1", "zoneA", "tagB")`
- **THEN** the registry row remains `"tagA"`
- **AND** the plugin logs `Logger.Notice` with the dropped pair and tag

### Requirement: SetAudioZoneEnable maps to Control.Set on the registered controlTag

`SetAudioZoneEnable(channelId, zoneId, enable)` SHALL look up the registered `controlTag` and enqueue `Control.Set { Name = controlTag, Value = enable }`. The cache for the pair SHALL be updated optimistically to `enable` before the wire send is attempted (M3 intent semantics).

#### Scenario: Set with enable=true sends Control.Set true

- **GIVEN** `("mic1", "zoneA") → "zone.mic1.A.enable"` is registered
- **WHEN** the framework calls `SetAudioZoneEnable("mic1", "zoneA", true)`
- **THEN** the plugin enqueues `Control.Set` with `Params: { Name: "zone.mic1.A.enable", Value: true }`
- **AND** `QueryAudioZoneEnable("mic1", "zoneA")` returns `true`

### Requirement: ToggleAudioZoneEnable reads the cache then sends the inverted value

`ToggleAudioZoneEnable(channelId, zoneId)` SHALL read the current cached boolean for the pair (default `false` for a never-seen pair), enqueue `Control.Set` with the inverted value, and update the cache to the inverted value. There is no QRC `Control.Toggle`; the toggle is a read-cache-flip-set sequence.

#### Scenario: Toggle from cached false produces Set true

- **GIVEN** the cache for `("mic1", "zoneA")` is `false`
- **WHEN** the framework calls `ToggleAudioZoneEnable("mic1", "zoneA")`
- **THEN** the plugin enqueues `Control.Set` with `Value = true`
- **AND** the cache updates to `true`

### Requirement: QueryAudioZoneEnable returns from the cache

`QueryAudioZoneEnable(channelId, zoneId)` SHALL return the cached value without enqueueing a wire request. Unknown pairs SHALL return `false` per the framework spec.

#### Scenario: Unknown pair returns false

- **GIVEN** no row for `("nope", "nope")` is registered
- **WHEN** the framework calls `QueryAudioZoneEnable("nope", "nope")`
- **THEN** the call returns `false`

### Requirement: AutoPoll deltas on a zone controlTag update the cache and fire AudioZoneEnableChanged

When an AutoPoll delta's `Name` matches a registered zone `controlTag`, the plugin SHALL resolve the owning `(channelId, zoneId)` pair, parse the boolean value (accepting bool, int, or float per the M3 mute extraction policy), update the cache, and — if the cache changed — fire `AudioZoneEnableChanged` with `(channelId, zoneId)` per the framework doc.

#### Scenario: Delta on a registered tag fires the event with channelId + zoneId

- **GIVEN** `("mic1", "zoneA") → "zone.mic1.A.enable"` is registered and the cache is `false`
- **WHEN** an AutoPoll response carries `Changes: [{ Name: "zone.mic1.A.enable", Value: true }]`
- **THEN** the cache for the pair updates to `true`
- **AND** `AudioZoneEnableChanged` fires once with args `("mic1", "zoneA")`

### Requirement: RemoveAudioZoneEnable drops the row without affecting peers

`RemoveAudioZoneEnable(channelId, zoneId)` SHALL remove the row keyed on the supplied pair. If no row matches, the call MUST be a silent no-op per the framework spec. Other rows for the same channel id (different zones) MUST remain.

#### Scenario: Remove drops only the named pair

- **GIVEN** the registry has `("mic1", "zoneA") → "tagA"` and `("mic1", "zoneB") → "tagB"`
- **WHEN** the framework calls `RemoveAudioZoneEnable("mic1", "zoneA")`
- **THEN** the registry row for `("mic1", "zoneA")` is gone
- **AND** the registry row for `("mic1", "zoneB")` remains
