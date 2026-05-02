# audio-control Specification

## Purpose
TBD - created by archiving change add-audio-control-and-presets. Update Purpose after archive.
## Requirements
### Requirement: IAudioControl level and mute calls map to QRC Control.Set and Control.Get

When the framework calls `SetAudioInputLevel(id, level)` or `SetAudioOutputLevel(id, level)`, the plugin SHALL look up the channel's `levelTag` from the `AudioChannelRegistry`, scale `level` from the framework 0–100 range to the device-native range using `levelMin` and `levelMax` registered for that channel, and enqueue a JSON-RPC `Control.Set` request with `Name = levelTag` and the scaled `Value`. The same mapping SHALL apply to mute via the channel's `muteTag` with `Value` `true` or `false`. `Get*` methods SHALL serve from the in-process cache populated by AutoPoll responses; they MUST NOT block on a wire round-trip.

#### Scenario: SetAudioInputLevel(50, channel with min=-80, max=0) sends Control.Set Value=-40

- **GIVEN** the registry has channel `mic1` with `levelTag = "mic1.gain"`, `levelMin = -80`, `levelMax = 0`
- **WHEN** the framework calls `SetAudioInputLevel("mic1", 50)`
- **THEN** the plugin enqueues a `Control.Set` request with `Params: { Name: "mic1.gain", Value: -40 }`

#### Scenario: GetAudioOutputMute returns from cache, not the wire

- **GIVEN** the cache for channel `out1` reports mute = true (last AutoPoll delta set it)
- **WHEN** the framework calls `GetAudioOutputMute("out1")`
- **THEN** the call returns `true` synchronously without enqueueing any wire request

### Requirement: Unknown channel id is a no-op for Set and returns 0 or false for Get

If `Set*` is called with an `id` that has not been registered via `AddInputChannel` or `AddOutputChannel`, the plugin SHALL log `Logger.Error` with the unknown id and return without enqueueing any wire request. If `Get*` is called with an unknown id, the plugin SHALL return `0` for level and `false` for mute, per `framework-docs/gcu-hardware-service/IAudioControl.md`.

#### Scenario: SetAudioInputLevel("nonexistent", 50) is silently dropped

- **GIVEN** the channel registry has no entry for `"nonexistent"`
- **WHEN** the framework calls `SetAudioInputLevel("nonexistent", 50)`
- **THEN** the plugin logs `Logger.Error` with the unknown id
- **AND** the command queue does not gain a new request

### Requirement: 0–100 framework levels round-trip through device-native scaling within ±1

The level scaler SHALL map framework 0–100 to and from `levelMin..levelMax` using half-up rounding such that `ToFramework(ToDevice(f, min, max), min, max) ∈ {f-1, f, f+1}` for every `f ∈ [0, 100]` and every `min < max`. A framework input outside `[0, 100]` MUST clamp to the boundary and log `Logger.Warn` once per offending channel id (subsequent out-of-range calls for the same id MUST NOT re-log).

#### Scenario: Round trip property holds across the full integer range

- **GIVEN** any `f ∈ [0, 100]` and any `min < max`
- **WHEN** the value is converted to device range and back
- **THEN** the result is within ±1 of `f`

### Requirement: RecallAudioPreset issues Snapshot.Load with bank and index from the registry

When the framework calls `RecallAudioPreset(id)`, the plugin SHALL look up `(bank, index)` registered via `AddPreset` and enqueue a JSON-RPC `Snapshot.Load` request with `Params: { Name: bank, Bank: index }`. The plugin MUST NOT include a `Ramp` field; the Core defaults to a zero-second ramp. An unknown preset id MUST log `Logger.Error` and return without enqueueing any request.

#### Scenario: Recall sends Snapshot.Load with the correct payload

- **GIVEN** preset `dinner` registered with `bank = "MainBank"`, `index = 3`
- **WHEN** the framework calls `RecallAudioPreset("dinner")`
- **THEN** the plugin enqueues a `Snapshot.Load` request with `Params: { Name: "MainBank", Bank: 3 }` and no `Ramp` field

### Requirement: AutoPoll deltas update the cache and fire the matching IAudioControl event

When the change-group manager parses an AutoPoll response that contains a `Changes` entry for a known control name, the plugin SHALL update the cached value for the owning channel id and, if the new value differs from the previous cached value, the plugin MUST raise the matching `IAudioControl` event (`AudioInputLevelChanged`, `AudioInputMuteChanged`, `AudioOutputLevelChanged`, or `AudioOutputMuteChanged`) with `(deviceId, channelId)` per `framework-docs/gcu-hardware-service/IAudioControl.md`.

#### Scenario: A mute=true delta on an output's muteTag fires AudioOutputMuteChanged

- **GIVEN** output channel `zone-a` has `muteTag = "zoneA.mute"` and cache reports mute = false
- **WHEN** an AutoPoll response arrives with `Changes: [{ Name: "zoneA.mute", Value: true }]`
- **THEN** the cache for `zone-a` updates to mute = true
- **AND** `AudioOutputMuteChanged` fires once with args `(dsp-id, "zone-a")`

#### Scenario: A delta whose value matches the cache fires no event

- **GIVEN** output channel `zone-a` cache already reports mute = true
- **WHEN** an AutoPoll response arrives with `Changes: [{ Name: "zoneA.mute", Value: true }]`
- **THEN** no event fires (the value did not change)

