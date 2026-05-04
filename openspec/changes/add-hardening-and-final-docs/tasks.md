# Tasks — M7 Hardening + Final Docs

## 1. Discharge deferred integration tests (M6 7.2 + 7.4)

- [ ] 1.1 `RedundancyEndToEndTests.Switchback_to_primary_when_it_returns_to_Active` — drive Standby then Active on the primary via dispatcher; assert subsequent write lands on primary's wire. Mirrors the existing failover test's dispatcher-driven shape.
- [ ] 1.2 `RedundancyEndToEndTests.Writes_during_double_Standby_window_are_refused_with_log` — push Standby on both via dispatcher; `RoutingQueue.TryEnqueue` returns false; verify `Logger.Error` was emitted via a captured log sink.
- [ ] 1.3 Tick the corresponding boxes in `openspec/changes/archive/2026-05-04-add-redundancy/tasks.md` §7.2 and §7.4 and reference the M7 archive in the inline note.

## 2. Mid-session control add (D-T2 / M3-6.3)

- [ ] 2.1 `AudioChannelRegistry.TryRegister` — when registry already has an active subscription (a `ChangeGroupManager` reference is live), call `ChangeGroupManager.AddControl(name)` + enqueue a one-shot `ChangeGroup.Poll`. When no active manager, behave as today (stage in registry, applied on next hydration).
- [ ] 2.2 Same wiring for `AudioZoneRegistry`, `LogicTriggerRegistry`. Refactor the common path into `IControlRegistry.RegisterMidSession(...)` if it doesn't bloat the surface.
- [ ] 2.3 `Mid_session_add_subscribes_new_control` integration test — connect, call `AddInputChannel`, observe an `AddComponentControl` + `Poll` on the wire.

## 3. ChangeGroup.Destroy on Disconnecting (D-T3 / M3-2.3)

- [ ] 3.1 `ConnectionManager` (or post-disconnect cleanup action): on entry to `Disconnecting`, if `_transport.IsConnected`, enqueue `ChangeGroup.Destroy` with the active group id; await one queue cycle (bounded by the existing drain timeout); proceed with disconnect regardless of outcome.
- [ ] 3.2 Test: `Destroy_is_attempted_when_transport_still_up` (unit, dispatcher-driven). Test: `Destroy_failure_does_not_block_disconnect` (unit, bounded queue refuses).

## 4. Threading shape — record D-T1, wire ThreadCensus (M2-7.3 / M3-4.5 / M3-4.6)

- [ ] 4.1 `ThreadCensus.Register` calls added at each steady-state Task-loop entry: send loop, receive loop, keepalive loop. Each loop unregisters on exit.
- [ ] 4.2 `ConnectionManager` `RunSessionAsync` does not register itself (it is the orchestrator, not a steady-state worker).
- [ ] 4.3 Update `SPEC_COMPLIANCE.md` row 4.1: deviation D-T1, "task-loops not threads, behaviour-equivalent under bounded steady-state work; see `design.md`".
- [ ] 4.4 `ThreadCensusTests.Reports_three_per_connected_manager` — pin the steady-state count to 3.

## 5. Public-API audit (D-T4)

- [ ] 5.1 Add `Microsoft.CodeAnalysis.PublicApiAnalyzers` (RS0016/RS0017 errors). Generate `PublicAPI.Shipped.txt` from the current surface. Empty `PublicAPI.Unshipped.txt`.
- [ ] 5.2 Sweep every `public` symbol; for each one not documented as test-only (e.g., `RedundantConnectionPair.Primary`/`Backup`), either (a) it has framework-consumer value — keep + ensure XML doc — or (b) downgrade to `internal` + add `InternalsVisibleTo("QscDspDevices.UnitTests")`. Record the verdict per symbol in this file's appendix.
- [ ] 5.3 XML doc completeness: `<GenerateDocumentationFile>true</GenerateDocumentationFile>` + `1591` (missing XML comment) is no longer suppressed. Build clean.
- [ ] 5.4 `[EditorBrowsable(EditorBrowsableState.Never)]` on public-but-not-for-consumers seams that resist `internal`-isation.

## 6. Logging + exception + redaction audit

- [ ] 6.1 Sweep `Logger.Error` / `Logger.Warn` / `Logger.Notice` / `Logger.Debug` call sites: every one carries `deviceId` + non-empty message. Lint via a local `grep` script that fails the build if any call site uses a string-empty literal.
- [ ] 6.2 `LogonAction` — in any debug-log of the outbound payload, redact the password field. Test: `Logon_payload_redacts_password_in_debug_log`.
- [ ] 6.3 Sweep all `[SuppressMessage("Design", "CA1031:...")]` justifications. For each, either (a) the catch is the right shape (callback seam, log-and-continue boundary) and the justification stays, or (b) narrow to a typed catch.
- [ ] 6.4 Audit: no test fixture installs a custom `Logger` sink that swallows errors silently. Tests that expect an error log either capture-and-assert or use the explicit `LogCapture` helper to be added in §6.5.
- [ ] 6.5 New test helper: `LogCapture` — temporarily routes `Logger.Error/Warn` to an in-memory list for the test's duration; restores prior sink in `Dispose`.

## 7. Property + mutation testing

- [ ] 7.1 FsCheck property: `SwitchbackPolicy.PickActive` is total (never throws) and idempotent under repeated calls with the same args.
- [ ] 7.2 FsCheck property: `RoutingCommandQueue.SetActive` then `TryEnqueue` is observably equivalent to direct `inner.TryEnqueue` for any active inner queue.
- [ ] 7.3 Stryker run on `Connectivity/Redundancy/*.cs` + `Protocol/CommandQueue.cs` + `Protocol/QrcFramer.cs`. Record kill rate in `REVIEW.md`. Target: ≥ 80 % kill on those four files; investigate misses.

## 8. Final documentation pass

- [ ] 8.1 `README.md` — quickstart (single Core), redundant wiring snippet, supported framework versions, deviation summary table.
- [ ] 8.2 `ARCHITECTURE.md` — sequence diagrams refreshed against the M6 routing-queue path; threading-shape diagram aligned with D-T1.
- [ ] 8.3 `SPEC_COMPLIANCE.md` — every `⚠` row resolves to either `✅` (work done in M7) or `❌ Dx` with rationale. No `⚠` rows remain.
- [ ] 8.4 `CHANGELOG.md` — first cut, M1 → M7. Format: keep-a-changelog. Public-API section sourced from `PublicAPI.Shipped.txt`.

## 9. Build, format, coverage, size gates

- [ ] 9.1 `dotnet build -c Release -warnaserror`: 0 warnings, 0 errors.
- [ ] 9.2 `dotnet format --verify-no-changes`: clean.
- [ ] 9.3 `dotnet test`: full matrix green, 5 consecutive runs.
- [ ] 9.4 Coverage gate raised 90 % → 92 % in CI workflow. Local merged coverage ≥ 93 %.
- [ ] 9.5 DLL size (`-c Release`) ≤ 500 KB. Recorded in `REVIEW.md`.
- [ ] 9.6 `openspec validate add-hardening-and-final-docs --strict`: passes.
- [ ] 9.7 Run `qsc-critic` agent locally; address blockers; record Pass 1 + Pass 2 in `REVIEW.md`.

## 10. Commit + PR + archive

- [ ] 10.1 Commit incrementally per top-level task group.
- [ ] 10.2 Open PR against `main`.
- [ ] 10.3 After merge, `openspec archive add-hardening-and-final-docs --yes`.

## Appendix — public-API verdicts (filled during §5.2)

To be populated during the audit. One row per public type / per public member that doesn't follow from its enclosing type.
