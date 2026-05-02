# Change: Add DSP logic trigger support

## Why

`IDspLogicTriggerSupport` is the smallest of the optional `IDsp`
side-interfaces — 2 methods + 1 event — but it's the last
DSP-control surface remaining before the M6 redundancy work and
the M7 polish. Plugins implementing this interface let the
framework fire QSC "logic trigger" controls (button-style named
controls that initiate side-effects in the design — start a
recording, fire a logic flow, advance a media player, etc.) and
observe their state transitions.

The protocol-level shape rides on the same QRC infrastructure M3
and M4 already wired:

- One change group (`qsc-plugin-state`) at 250 ms AutoPoll. M5
  grows the subscribed-control list by every registered logic
  trigger; the cap-at-4-groups budget is unaffected.
- `Control.Set` for outbound writes. `PulseDspLogicTrigger` sends
  `Control.Set { Name = tagName, Value = true }` — QSC trigger
  controls auto-reset on the design side, so a single `true` write
  is the momentary-pulse semantic.
- AutoPoll deltas on a registered trigger tag fire the
  `DspLogicTriggerStateChanged` event with `(triggerId)` per
  `framework-docs/gcu-hardware-service/IDspLogicTriggerSupport.md`.
  Unlike the audio events (Dual args), this one is Single — just
  the trigger id; no device id second arg.

## What Changes

- **New capability spec `logic-triggers`.** Defines the QRC
  mapping for `AddDspLogicTrigger` (registry insertion) and
  `PulseDspLogicTrigger` (`Control.Set true` against the
  registered tag), the AutoPoll-driven event raise contract, and
  the unknown-id semantics ("Pulse on an unknown id is a silent
  no-op + Logger.Error"; matches M3 / M4 conventions).
- **Modified capability spec `change-group-subscriptions`.**
  Hydration's subscribe list now also enqueues
  `ChangeGroup.AddControl` for every registered logic-trigger
  tag, alongside the M3 level/mute and M4 router/zone tags. Still
  one group; cap-at-4 unaffected.

- **Source code (under `src/QscDspDevices/`):**
  - `LogicTriggers/LogicTriggerRegistry.cs` — thread-safe
    `id → tagName` map with reverse `tagName → id` for AutoPoll
    dispatch. `Add(id, tagName)` (duplicate id replaces and logs
    `Logger.Notice` per the established M3/M4 shape; the framework
    spec doesn't say what to do, so we mirror the pattern), `TryGet`,
    `IsTriggerTag`, `GetAll`.
  - `LogicTriggers/LogicTriggerService.cs` — `Pulse(id)` enqueues
    `Control.Set { Name=tagName, Value=true }`; `OnDeviceUpdate`
    raises `LogicTriggerStateChanged(id)` on every AutoPoll delta
    (no cache because the framework's
    `DspLogicTriggerStateChanged` event signals "the trigger's
    state changed", not "the new state value"; coalescing on
    cached value would suppress legitimate same-state pulses on
    QSC designs that do not auto-reset).
  - Extend `AudioControl/AudioControlServiceFanout.cs` to a
    four-way dispatch (router → zone → trigger → audio control),
    O(1) per branch via the existing `IsRouterTag` /
    `IsZoneTag` predicates plus a new `IsTriggerTag`.
  - `Connectivity/PostConnect/HydrateChangeGroupAction.cs`
    extended to subscribe every registered logic-trigger tag
    alongside the M3+M4 surface. Backwards-compat ctors preserved.
  - `Plugin/QscDspTcp.cs` — wires the trigger service in
    `Initialize`; replaces the M2-stub `AddDspLogicTrigger` and
    `PulseDspLogicTrigger` bodies with delegate-to-service; raises
    `DspLogicTriggerStateChanged`. CS0067 narrowed to just the M6
    events (redundancy + backup-device-connection).

- **Tests:**
  - Unit: `LogicTriggerRegistryTests` (Add/replace/lookup,
    `IsTriggerTag` predicate); `LogicTriggerServiceTests` (Pulse
    enqueues correct `Control.Set true`; AutoPoll delta fires
    event; unknown-id Pulse logs error and is silent; AutoPoll on
    unknown tag is silent).
  - Fanout test extension: trigger-tag dispatches to trigger
    service; precedence pinned (router → zone → trigger →
    audio).
  - Integration: `PulseDspLogicTrigger_round_trips_via_Control_Set_true`
    and `Server_pushed_AutoPoll_on_triggerTag_fires_DspLogicTriggerStateChanged`
    against the FakeQrcServer.

- **Documentation:**
  - `ARCHITECTURE.md` post-connect hydration section: trigger
    tags added to the subscribe list.
  - `SPEC_COMPLIANCE.md`: discharge the logic-trigger row(s) the
    README requires.

## Scope-edge decisions

1. **Pulse = `Control.Set true` only.** No follow-up `Value=false`
   write. QSC trigger controls in Designer typically auto-reset on
   the design side after the action fires; sending the reset would
   be redundant at best, race-y at worst. If a particular design
   needs a stretched-pulse pattern, that's a Designer-side
   configuration concern.
2. **No cache on the trigger service.** The framework's
   `DspLogicTriggerStateChanged` event signals "this trigger
   transitioned", not "the new value is X" (Single-arg event, just
   the id). Coalescing on a cached boolean would suppress
   legitimate consecutive pulses on a momentary trigger that holds
   `true` briefly. The service raises the event on every AutoPoll
   delta for a registered trigger tag.
3. **Duplicate `Add` replaces.** Spec is silent. We mirror the M3
   `AudioChannelRegistry` shape (replace + `Logger.Notice`).
   Symmetric, predictable, and easier to reason about than the
   M4 zone-spec's mandated drop-on-duplicate.
4. **Fanout precedence: router → zone → trigger → audio.** Trigger
   slots in before audio because triggers are typically named
   distinctly from level/mute tags; a tag claimed as both a trigger
   AND an audio control is a Designer-side error and triggering
   wins (the more-specific feature). Pinned in tests.
