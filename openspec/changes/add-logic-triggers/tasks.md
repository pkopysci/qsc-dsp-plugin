# Tasks — add-logic-triggers

## 1. Logic-trigger core

- [ ] 1.1 `LogicTriggers/LogicTriggerRegistry.cs` — thread-safe `id → tagName` map with reverse `tagName → id`. `Add(id, tagName)` (replace-on-duplicate with `Logger.Notice`), `TryGet`, `IsTriggerTag(string)`, `GetAll`.
- [ ] 1.2 `LogicTriggers/LogicTriggerService.cs` — `Pulse(id)` enqueues `Control.Set { Name=tagName, Value=true }`; `OnDeviceUpdate(ChangeGroupDelta)` raises `LogicTriggerStateChanged(id)` on every delta for a registered tag (no cache; per spec the event signals "transitioned", not "the value is X").

## 2. Fanout extension

- [ ] 2.1 Extend `AudioControl/AudioControlServiceFanout.cs` to a four-way dispatch: router → zone → trigger → audio. Optional ctor parameter for the trigger registry + service so M3-only constructions keep working in unit tests.

## 3. Hydration extension

- [ ] 3.1 `Connectivity/PostConnect/HydrateChangeGroupAction.cs` — extend the iteration to also enqueue `ChangeGroup.AddControl` for every registered trigger tag. Backwards-compat ctor preserved.

## 4. QscDspTcp surface

- [ ] 4.1 Wire `IDspLogicTriggerSupport.AddDspLogicTrigger` and `PulseDspLogicTrigger` to the registry + service. Forward `LogicTriggerStateChanged` to `QscDspTcp.DspLogicTriggerStateChanged`. Remove the CS0067 suppression for that event.
- [ ] 4.2 Update `Initialize` composition to construct + wire the trigger service, registry, and four-way fanout.

## 5. Tests — unit

- [ ] 5.1 `LogicTriggerRegistryTests` — Add / TryGet / IsTriggerTag happy paths; replace-on-duplicate with Notice log.
- [ ] 5.2 `LogicTriggerServiceTests` — Pulse enqueues `Control.Set { Value=true }`; AutoPoll fires event with the trigger id; unknown id Pulse logs error and is silent; AutoPoll on unknown tag is silent.
- [ ] 5.3 `AudioControlServiceFanoutTests` extension — trigger-tag dispatches to trigger service; precedence pinned.

## 6. Tests — integration

- [ ] 6.1 `PulseDspLogicTrigger_round_trips_via_Control_Set_true`.
- [ ] 6.2 `Server_pushed_AutoPoll_on_triggerTag_fires_DspLogicTriggerStateChanged`.

## 7. Documentation

- [ ] 7.1 Update `ARCHITECTURE.md` post-connect section: hydration now subscribes trigger tags alongside level/mute/router/zone.
- [ ] 7.2 Update `SPEC_COMPLIANCE.md`: discharge the README rows for logic triggers + the `DspLogicTriggerStateChanged` event.

## 8. Build, format, and review gates

- [ ] 8.1 `dotnet build`: 0 warnings, 0 errors (Debug + Release).
- [ ] 8.2 `dotnet format --verify-no-changes`: clean.
- [ ] 8.3 `dotnet test`: full matrix green, 3 consecutive runs.
- [ ] 8.4 Coverage on `QscDspDevices.dll`: ≥ 90 % line.
- [ ] 8.5 DLL size (`-c Release`): ≤ 500 KB.
- [ ] 8.6 `openspec validate add-logic-triggers --strict`: passes.
- [ ] 8.7 Run `qsc-critic` agent locally; save report to this change's `REVIEW.md`. Address blockers before opening the PR.

## 9. Commit + PR

- [ ] 9.1 Commit incrementally — registry + service, fanout extension, hydration, surface wiring, tests, docs.
- [ ] 9.2 Open PR against `main`.
