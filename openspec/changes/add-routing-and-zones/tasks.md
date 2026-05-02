# Tasks — add-routing-and-zones

## 1. Audio routing core

- [x] 1.1 Extend `AudioControl/AudioChannelRegistry.cs` with a `bankIndex → channelId` reverse map maintained on every register/replace. Add `TryGetChannelByBankIndex(int, out AudioChannel?)`. Remove the stale entry on re-registration of the same channel id with a new bankIndex.
- [x] 1.2 Add `IsRouterTag(string)` and `IsZoneTag(string)` predicates to the registry / zone registry respectively, used by the AutoPoll fan-out dispatcher.
- [x] 1.3 `AudioControl/AudioRoutingService.cs` — owns the `outputId → currentSourceId` cache (string-typed; empty string is "no source"). `Route(sourceId, outputId)`, `Clear(outputId)`, `GetCurrentSource(outputId)`. `OnDeviceUpdate(ChangeGroupDelta)` parses the routerTag's AutoPoll value, resolves bankIndex back to channelId, updates the cache, fires `AudioRouteChanged` on transitions.
- [x] 1.4 Per the framework doc: `GetCurrentAudioSource` returns empty string when the query "fails" — interpret as "no cached value" (M3-style hydration window) AND "cleared (cache says 0)".

## 2. Audio zones core

- [x] 2.1 `AudioControl/AudioZoneRegistry.cs` — thread-safe `(channelId, zoneId) → controlTag` map. `Add`, `Remove`, `TryGet`, `GetAll`, `IsZoneTag(string)`. Per framework spec, `Add` ignores duplicates (matching `(channelId, zoneId)`); we log `Logger.Notice` on drop.
- [x] 2.2 `AudioControl/AudioZoneEnableService.cs` — `Set(channelId, zoneId, bool)`, `Toggle(channelId, zoneId)`, `Query(channelId, zoneId)`. `Toggle` reads cached value, sends `Control.Set` with inverted, updates cache. `Query` serves from cache (false for unknown pair).
- [x] 2.3 `OnDeviceUpdate(ChangeGroupDelta)` in the zone service: parse the controlTag's AutoPoll value (boolean / int / float same as mute extraction), update cache, fire `AudioZoneEnableChanged`. Per framework doc the args are `(channelId, zoneId)` not `(deviceId, zoneId)` — pin in tests.

## 3. AutoPoll fan-out

- [x] 3.1 `AudioControl/AudioControlServiceFanout.cs` (or rewrite the M3 `groupManager.SetDeltaCallback` wiring inline in `QscDspTcp.Initialize`) — three-way dispatch: route → router service, zone-tag → zone service, otherwise → audio-control service. Priority order matters; document and test it.

## 4. Hydration extension

- [x] 4.1 `Connectivity/PostConnect/HydrateChangeGroupAction.cs` — extend the iteration to also enqueue `ChangeGroup.AddControl` for every output's `routerTag` (when non-empty) and every registered zone-enable `controlTag`.
- [x] 4.2 The 4-group cap is unaffected (still one group); confirm in tests by counting `GroupCount` after a typical hydration.

## 5. QscDspTcp surface

- [x] 5.1 Wire `IAudioRoutable.RouteAudio` / `ClearAudioRoute` / `GetCurrentAudioSource` to `AudioRoutingService`. Forward the service's `RouteChanged` event to `QscDspTcp.AudioRouteChanged`. Remove the `CS0067` suppression for that event.
- [x] 5.2 Wire the 5 `IAudioZoneEnabler` methods to `AudioZoneEnableService` + `AudioZoneRegistry`. Forward the zone-changed event. Remove the `CS0067` suppression.
- [x] 5.3 Update the M3 `Initialize` composition to construct + wire the routing + zone services and the fan-out dispatcher.

## 6. Tests — unit

- [x] 6.1 `AudioChannelRegistryTests` extension — `bankIndex → channelId` reverse map happy path; re-registration with new bankIndex remaps; `IsRouterTag` predicate.
- [x] 6.2 `AudioZoneRegistryTests` — Add/Remove/TryGet; duplicate-`Add` drop with Notice log; `IsZoneTag` predicate.
- [x] 6.3 `AudioRoutingServiceTests` — `Route` enqueues `Control.Set { Name=routerTag, Value=bankIndex }`; `Clear` sends `Value=0`; `GetCurrentSource` from cache (empty before any update); AutoPoll delta on routerTag updates cache + fires event with correct args; unknown-output Set/Clear logs error and returns silently; unknown-source Set logs error.
- [x] 6.4 `AudioZoneEnableServiceTests` — `Set` enqueues `Control.Set` with bool value; `Toggle` reads cache then sends inverted; `Query` from cache; AutoPoll fires event with `(channelId, zoneId)` args; unknown pair Set/Toggle/Query is silent + returns false respectively.
- [x] 6.5 `AudioControlServiceFanoutTests` — three-way dispatch precedence; tag in two registries (config error) goes to first match.

## 7. Tests — integration (xUnit + FakeQrcServer)

- [x] 7.1 `RouteAudio_round_trips_via_Control_Set_on_routerTag`.
- [x] 7.2 `Server_pushed_AutoPoll_on_routerTag_fires_AudioRouteChanged`.
- [x] 7.3 `SetAudioZoneEnable_round_trips_via_Control_Set_on_controlTag`.

## 8. Documentation

- [x] 8.1 Update `ARCHITECTURE.md` post-connect section: hydration now subscribes routerTags + zone-controlTags in addition to level/mute tags.
- [x] 8.2 Update `SPEC_COMPLIANCE.md`: discharge rows 5.4 (matrix routing), 6.4 (numeric matrix-router controls), the `AudioRouteChanged` and `AudioZoneEnableChanged` event rows.

## 9. Build, format, and review gates

- [x] 9.1 `dotnet build`: 0 warnings, 0 errors (Debug + Release).
- [x] 9.2 `dotnet format --verify-no-changes`: clean.
- [x] 9.3 `dotnet test`: full matrix green, 3 consecutive runs, no flakes.
- [x] 9.4 Coverage on `QscDspDevices.dll`: ≥ 90 % line.
- [x] 9.5 DLL size (`-c Release`): ≤ 500 KB.
- [x] 9.6 `openspec validate add-routing-and-zones --strict`: passes.
- [ ] 9.7 Run `qsc-critic` agent locally; save report to this change's `REVIEW.md`. Address blockers before opening the PR.

## 10. Commit + PR

- [x] 10.1 Commit incrementally — one logical commit per major component (registry, services, fan-out, hydration, surface, tests, docs).
- [ ] 10.2 Open PR against `main`. Push + PR creation gated by user approval.
