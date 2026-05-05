# Tasks — M-ECP part 3

## 1. AutoPoll hydration

- [ ] 1.1 `Connectivity/Ecp/EcpHydrateAction.cs` — runs after auth in `RunSessionAsync`. Emits `cgc 1` then `cga 1 "<tag>"` for every named control across the three registries, then `cgsna 1 2000`.
- [ ] 1.2 Wire `EcpHydrateAction` into `EcpConnectionManager.RunSessionAsync` between auth and `_queue.StartAccepting()`.
- [ ] 1.3 Unit tests: `EcpHydrateActionTests` — no-controls case; mixed-registry case; ordering invariant (cgc before cga before cgsna).

## 2. AutoPoll subscription / cv → service tier

- [ ] 2.1 `Connectivity/Ecp/EcpAutoPollSubscription.cs` — subscribes to `EcpDispatcher.ResponseReceived`, filters `cv` lines, dispatches to the appropriate ECP service.
- [ ] 2.2 `EcpAudioControlService` gains `OnInboundLevel(string controlTag, double rawValue)` and `OnInboundMute(...)` that the bridge calls; reconciles the cache and re-fires the change event with the Core's authoritative value.
- [ ] 2.3 Same shape on `EcpAudioRoutingService.OnInboundRoute(...)`, `EcpAudioZoneEnableService.OnInboundZone(...)`, `EcpLogicTriggerService.OnInboundTrigger(...)`.
- [ ] 2.4 Wire the subscription into `QscDspTcp.InitializeEcp`. Disposable lifetime — torn down with the connection.
- [ ] 2.5 Unit tests for the bridge: `cv "tag" "..." V P` → correct service callback; unknown tag → log Warn + drop.

## 3. Redundancy via sg-poll

- [ ] 3.1 `Connectivity/Ecp/EcpEngineStateProbe.cs` — schedules `sg` every 2 s; parses `sr`; raises `StateChanged(EngineState)` events. Mirrors `EngineStatusObserver`'s public surface.
- [ ] 3.2 `EcpRedundantPairBuilder` (or inline in `QscDspTcp`) — composes the M6 `RedundantConnectionPair` against the per-side `EcpConnectionManager` + `EcpCommandQueue` + `EcpEngineStateProbe`. Reuses `SwitchbackPolicy` unchanged.
- [ ] 3.3 `QscDspTcp.SetBackupDeviceConnection` — replace the part-2 ECP-pair `Logger.Notice` refusal with the actual pair construction. Mixed-protocol refusal stays.
- [ ] 3.4 Unit test: `EcpEngineStateProbe` raises `Active` then `Standby` as `IS_ACTIVE` flips on the simulated wire.

## 4. Integration tests

- [ ] 4.1 `tests/QscDspDevices.IntegrationTests/Ecp/EcpHydrationEndToEndTests.cs` — connect against `FakeEcpServer`, register controls, verify the hydrate sequence on the wire.
- [ ] 4.2 `tests/QscDspDevices.IntegrationTests/Ecp/EcpAutoPollEndToEndTests.cs` — server pushes a `cv` (via a new `FakeEcpServer.PushControlValue` knob); client cache reflects it.
- [ ] 4.3 `tests/QscDspDevices.IntegrationTests/Ecp/EcpRedundancyEndToEndTests.cs` — two FakeEcpServers, drive `SetActive(true/false)` on each, assert routing flips.

## 5. Spec compliance + docs

- [ ] 5.1 `SPEC_COMPLIANCE.md` row 3.1 — drop the "M-ECP-part-3 deferred" note.
- [ ] 5.2 `QUICKSTART.md` — drop the "deferred" caveat on ECP redundancy in the support table.
- [ ] 5.3 `CHANGELOG.md` — M-ECP-part-3 entry under Unreleased.

## 6. Build, format, coverage, size gates

- [ ] 6.1 `dotnet build -c Release`: 0 warnings, 0 errors.
- [ ] 6.2 `dotnet format --verify-no-changes`: clean.
- [ ] 6.3 `dotnet test`: full matrix green per-suite; 5 consecutive runs.
- [ ] 6.4 Local merged line coverage ≥ 88 % (the M-ECP-part-2-set floor).
- [ ] 6.5 DLL size (`-c Release`) ≤ 500 KB.
- [ ] 6.6 `openspec validate add-ecp-redundancy-and-autopoll --strict`: passes.
- [ ] 6.7 Run `qsc-critic` agent locally; address blockers; record Pass 1 + Pass 2 in `REVIEW.md`.
- [ ] 6.8 Public-surface snapshot unchanged.

## 7. Commit + PR + archive

- [ ] 7.1 Commit incrementally per top-level task group.
- [ ] 7.2 Open PR against `main`.
- [ ] 7.3 After merge, `openspec archive add-ecp-redundancy-and-autopoll --yes`.
