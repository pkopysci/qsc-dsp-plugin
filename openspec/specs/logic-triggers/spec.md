# logic-triggers Specification

## Purpose
TBD - created by archiving change add-logic-triggers. Update Purpose after archive.
## Requirements
### Requirement: AddDspLogicTrigger registers id-to-tagName mapping

When the framework calls `AddDspLogicTrigger(id, tagName, tags)`, the plugin SHALL register the `id → tagName` pair in `LogicTriggerRegistry` and add the tag to the AutoPoll subscription set on the next hydration. If a trigger with the same `id` is already registered, the new registration SHALL replace the prior entry and log `Logger.Notice` (per the M3 `AudioChannelRegistry` shape; the framework spec does not mandate drop-on-duplicate for triggers).

#### Scenario: Add then re-Add with the same id replaces the prior tag

- **GIVEN** the registry is empty
- **WHEN** the framework calls `AddDspLogicTrigger("rec", "rec.start", [])` then `AddDspLogicTrigger("rec", "rec.start.v2", [])`
- **THEN** `TryGet("rec", out tag)` returns `true` and `tag = "rec.start.v2"`
- **AND** the plugin logs `Logger.Notice` on the second call

### Requirement: PulseDspLogicTrigger sends Control.Set with Value true

`PulseDspLogicTrigger(id)` SHALL look up the registered `tagName` and enqueue `Control.Set { Name = tagName, Value = true }`. No follow-up `Value = false` write is sent — QSC trigger controls auto-reset on the design side. Unknown `id` MUST log `Logger.Error` and be a silent no-op (no exception, no event surfaced).

#### Scenario: Pulse known id sends Control.Set true

- **GIVEN** trigger `rec` is registered with `tagName = "rec.start"`
- **WHEN** the framework calls `PulseDspLogicTrigger("rec")`
- **THEN** the plugin enqueues `Control.Set` with `Params: { Name: "rec.start", Value: true }`

#### Scenario: Pulse unknown id logs error and does not enqueue

- **GIVEN** the registry has no row for `"nope"`
- **WHEN** the framework calls `PulseDspLogicTrigger("nope")`
- **THEN** the plugin logs `Logger.Error`
- **AND** the command queue does not gain a new request

### Requirement: AutoPoll deltas on a registered trigger tag fire DspLogicTriggerStateChanged

When an AutoPoll delta's `Name` matches a registered trigger `tagName`, the plugin SHALL fire `DspLogicTriggerStateChanged` with `(triggerId)` per `framework-docs/gcu-hardware-service/IDspLogicTriggerSupport.md`. The event SHALL fire on every delta — the service does NOT cache the trigger value because the framework event is Single-arg ("transitioned"), not Dual ("the new value is X"); coalescing on a cached value would suppress legitimate consecutive pulses on a momentary trigger that holds `true` briefly.

#### Scenario: Two consecutive deltas fire two events

- **GIVEN** trigger `rec` is registered with `tagName = "rec.start"`
- **WHEN** an AutoPoll response carries `Changes: [{ Name: "rec.start", Value: true }]` followed by a second AutoPoll with the same delta
- **THEN** `DspLogicTriggerStateChanged` fires twice, both with arg `"rec"`

### Requirement: Fanout precedence places trigger lookup before audio control

The `AudioControlServiceFanout` four-way dispatch order SHALL be `router → zone → trigger → audio`. A tag claimed by both a registered trigger AND an audio level/mute / preset / route control is a Designer-side configuration error; the fanout SHALL dispatch it to the trigger service rather than the audio service.

#### Scenario: Tag registered as both trigger and level falls to trigger

- **GIVEN** trigger `rec` is registered with `tagName = "shared.tag"` and channel `mic1` is registered with `levelTag = "shared.tag"`
- **WHEN** an AutoPoll delta arrives on `"shared.tag"`
- **THEN** `DspLogicTriggerStateChanged` fires
- **AND** `AudioInputLevelChanged` does NOT fire

