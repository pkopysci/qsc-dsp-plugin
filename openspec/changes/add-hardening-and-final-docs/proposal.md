# M7 — Hardening, size budget, final docs

## Why

M2–M6 delivered every advertised feature path. Coverage sits at 91 %, the Release DLL is ~110 KB / 500 KB, and every milestone passed a critic round. What remains is the gap between *"works"* and *"safe to hand to a stranger"* — a finish-pass that closes the deferred items from the prior milestones, audits the public API for semver and doc completeness, tightens the logging/exception/threading invariants the framework spec actually cares about, and makes the README + ARCHITECTURE + SPEC_COMPLIANCE coherent.

This is not a feature milestone. No new public surface. No new protocol path (ECP is the next milestone, not this one).

## What changes

1. **Discharge deferred work that is small + cheap.**
   - **M6-7.4** — `Writes_during_double_Standby_window_are_refused_with_log` integration test.
   - **M6-7.2** — `Switchback_to_primary_when_it_returns_to_Active` integration test (synthesised through the dispatcher to avoid the cold-start race that flaked the original M6 attempt).
   - **M3-2.3** — `ChangeGroup.Destroy` write on `Disconnecting`, gated by a "transport still up" check so it doesn't race the disconnect.
   - **M3-6.3** — mid-session `AddInputChannel` / `AddOutputChannel` / `AddPreset` subscribes the new control to the existing change group (or refuses with a clear log if no session).

2. **Formally close — or formally abandon — the M2/M3 threading deferrals.**
   - **M2-7.3 / M3-4.5 / M3-4.6** — decide whether the `RunSessionAsync` + Task-loop shape stays (recorded as deviation D-T1 in SPEC_COMPLIANCE) or whether we collapse to the Thread-trio README §4 prescribes. The decision is a one-way door for semver, so it lands in M7 and not later.
   - `ThreadCensus` registers every steady-state task it actually owns, so the runtime guard reports the truth.

3. **Public-API audit.**
   - Every `public` symbol on `QscDspDevices.dll` has a non-empty XML doc comment.
   - `[EditorBrowsable(EditorBrowsableState.Never)]` on internal-but-`public`-for-tests seams (or move them to `internal` + `InternalsVisibleTo` if they were never meant for consumers).
   - Add `PublicApiGenerator` snapshot tests so M-ECP and beyond cannot accidentally break the surface.

4. **Logging + exception + redaction audit.**
   - Every `Logger.Error` carries the `deviceId`, an event tag, and a non-empty message; every `catch` block either re-throws, logs, or has a written justification.
   - `LogonAction` redacts password from the request envelope before any debug log of the outbound payload.
   - Sweep all `CA1031` suppressions; either justify in code or remove.

5. **Mutation + property-test gap closure (advisory, not blocking).**
   - Run Stryker against the QRC framer, command queue, switchback policy, and routing facade with reasonable mutators; record kill rate.
   - One additional FsCheck property per deterministic state machine (`SwitchbackPolicy.PickActive`, `RoutingCommandQueue.SetActive`).

6. **Final docs pass.**
   - `README.md` — quickstart, supported framework versions, single + redundant wiring snippets, deviation summary.
   - `ARCHITECTURE.md` — refresh sequence diagrams to match the M6 routing-queue path.
   - `SPEC_COMPLIANCE.md` — every row currently `⚠` either resolves to `✅` (work done in M7) or to `❌ deviation Dx` (recorded with rationale).
   - `CHANGELOG.md` — first cut spanning M1–M7.

7. **Quality gates.**
   - Coverage gate raised 90 % → 92 % (current: 91 %; M7 closes the gap).
   - DLL size budget re-asserted ≤ 500 KB.
   - `dotnet build -warnaserror` and `dotnet format --verify-no-changes` clean.
   - `qsc-critic` Pass 1 + 2 documented in `REVIEW.md`.

## Impact

- **Specs touched:** `project-foundation` (gate numbers + docs), `threading-budget` (decision recorded), `connection-manager` (mid-session add + destroy-on-disconnect), `qrc-protocol` (logon redaction).
- **No new specs.**
- **Public API:** zero net additions. Possible reductions (via `EditorBrowsable` or `internal`) flagged in tasks.md per symbol.
- **Risk:** the M7 threading decision is irreversible without a major bump. Documented in `design.md`.
