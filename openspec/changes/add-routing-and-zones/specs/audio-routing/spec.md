# Audio Routing — Spec Delta

## ADDED Requirements

### Requirement: RouteAudio maps to Control.Set on the output's routerTag

When the framework calls `RouteAudio(sourceId, outputId)`, the plugin SHALL look up the output channel's `routerTag` from `AudioChannelRegistry`, resolve `sourceId` to its `bankIndex` via the same registry, and enqueue a JSON-RPC `Control.Set` request with `Name = routerTag` and `Value = bankIndex`. The cache for `outputId` SHALL be updated optimistically to `sourceId` before the wire send is attempted, mirroring the M3 audio-control intent semantics.

#### Scenario: RouteAudio with known source and output sends Control.Set with the bank index

- **GIVEN** input channel `mic1` registered with `bankIndex = 3`, output `out1` registered with `routerTag = "mixer.out1.source"`
- **WHEN** the framework calls `RouteAudio("mic1", "out1")`
- **THEN** the plugin enqueues a `Control.Set` request with `Params: { Name: "mixer.out1.source", Value: 3 }`
- **AND** `GetCurrentAudioSource("out1")` returns `"mic1"` immediately

### Requirement: ClearAudioRoute sends value 0 on the routerTag

When the framework calls `ClearAudioRoute(outputId)`, the plugin SHALL look up the output's `routerTag` and enqueue a `Control.Set` request with `Value = 0`. The cache for `outputId` SHALL be updated to the empty string immediately, matching the framework's "no source" return contract on `GetCurrentAudioSource`.

#### Scenario: ClearAudioRoute zeroes the routerTag and the cache

- **GIVEN** output `out1` is currently routed to `mic1`
- **WHEN** the framework calls `ClearAudioRoute("out1")`
- **THEN** the plugin enqueues `Control.Set` with `Params: { Name: "mixer.out1.source", Value: 0 }`
- **AND** `GetCurrentAudioSource("out1")` returns the empty string

### Requirement: GetCurrentAudioSource returns from the cache

`GetCurrentAudioSource(outputId)` SHALL return the cached source channel id without enqueueing a wire request. Unknown output ids SHALL return the empty string. An output whose AutoPoll cache is unpopulated (e.g. before the first AutoPoll response after Connect) SHALL also return the empty string. Routing controls reflecting source value `0` (the QSC "cleared" sentinel) SHALL surface as the empty string.

#### Scenario: Get for an unknown output returns empty

- **GIVEN** the registry has no output `nope`
- **WHEN** the framework calls `GetCurrentAudioSource("nope")`
- **THEN** the call returns the empty string

### Requirement: AutoPoll deltas on a routerTag update the cache and fire AudioRouteChanged

When an AutoPoll response carries a delta whose `Name` matches an output's `routerTag`, the plugin SHALL parse the integer value, resolve it to a source channel id via the bank-index reverse map (or empty string when the value is `0` or unmapped), update the cached `outputId → sourceId`, and — if the cache value changed — fire `AudioRouteChanged` with `(deviceId, outputId)` per `framework-docs/gcu-hardware-service/IAudioRoutable.md`.

#### Scenario: Delta with bankIndex 5 fires AudioRouteChanged when 5 maps to mic2

- **GIVEN** output `out1` cached source is `""` and input `mic2` is registered with `bankIndex = 5`
- **WHEN** an AutoPoll response carries `Changes: [{ Name: "mixer.out1.source", Value: 5 }]`
- **THEN** the cache for `out1` updates to `"mic2"`
- **AND** `AudioRouteChanged` fires once with args `(dsp-id, "out1")`
