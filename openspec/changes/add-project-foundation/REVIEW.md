# QSC Critic Review — add-project-foundation / main (uncommitted)
**Date:** 2026-04-29 UTC
**Build:** green — `dotnet build QscDspDevices.sln` 0 warnings, 0 errors (Debug and Release)
**Tests:** none yet (M1 scaffolding only — test projects compile but contain no tests). Coverage: n/a.
**DLL size (Release):** 4.5 KB / 500 KB budget (QscDspDevices.csproj has no production source yet).
**OpenSpec validation:** `openspec validate add-project-foundation --strict` passes.
**Verdict:** ⚠ revise

The foundation is unusually thorough for an M1 — strict analyzers, central package management, audit-trail headers on every stub, deviation citations with real URLs, and a solid layering plan. But several gates that the README and project.md *claim* to enforce are not actually enforced by CI or the script that purports to enforce them. Those should be tightened before locking M1 in, because every later milestone will inherit the false-confidence signal.

---

## Blockers (must fix before merge)

1. **DLL-size budget allows ~512 KB through (`.github/workflows/ci.yml:62-76`)** — `SIZE_KB=$(( SIZE_BYTES / 1024 ))` is integer division, so a DLL up to `501 * 1024 - 1 = 513,023` bytes (≈ 501.0 KB) will still report `500 KB` and PASS. The README budget is 500 KB. The "500 KB" line in the CI summary is misleading because the underlying check is wrong by ~13 KB.
   - Why it matters: this is the single most-cited number in the README. The whole point of the M1 foundation is that the budget is mechanically enforced. Right now it's mechanically enforced *imprecisely*, and the imprecision favours us — exactly the slop signature the third-party reviewer is looking for.
   - Suggested fix: compare in bytes, not KB. Replace the math with `BUDGET_BYTES=$((500*1024)); if [ $SIZE_BYTES -gt $BUDGET_BYTES ]; then …` and keep the KB display for the summary table only.

2. **`openspec validate` job is a no-op when the CLI install fails (`.github/workflows/ci.yml:205-230`)** — `npm install -g @openspec/cli` is wrapped in `continue-on-error: true`, then the script runs `if command -v openspec; then ... ; else echo "::warning::openspec CLI not available; skipping spec validation."; fi`. If the package name is wrong (it likely is — see Concern 1), the install silently fails, the validate step prints a warning, and the job completes green. The reviewer will see ✅ on a PR whose proposal is malformed.
   - Why it matters: tasks.md item 7.1 explicitly requires `openspec validate add-project-foundation --strict` to pass. Today it passes locally but is unenforced on CI.
   - Suggested fix: either pin the install (`npx -y @openspec/cli` or use the actual published package name once verified), or fail the job if `command -v openspec` returns false. Drop `continue-on-error` from the install step.

3. **Stub `Vector2D` static directional vectors return `[0, 0]` (`src/FrameworkStubs/CommonUtils/DataObjects/Vector2D.cs:10-14`)** — `Vector2D.Up`/`Down`/`Left`/`Right` are declared `static readonly Vector2D Up = new();` with no field initialization, so `Up.Y` is `0` against the stub but the framework docs say `Up = [0, 1]`. This is exactly the "stub drift" the design.md explicitly warned against ("If the stub signatures diverge from the real package public surface, integration will break"). The framework-stubs spec says trivial returns are permitted "where the documentation explicitly defines a default" — Up/Down/Left/Right have explicitly-defined values that the stub silently changes.
   - Why it matters: any production code that reads `Vector2D.Up.Y` for, say, a coordinate-comparison heuristic will pass tests against the stub and silently break against the real DLL. The plugin probably won't touch Vector2D, but the bet is about audit-friendliness, and this is an audit finding the reviewer will catch.
   - Suggested fix: initialize each as `new Vector2D { X = 0, Y = 1 }` etc., per the documented values. Add a `// Spec: Up = [0, 1]` end-of-line comment to make the intent obvious.

---

## Concerns (should fix; not strictly blocking)

1. **`@openspec/cli` package name is unverified (`.github/workflows/ci.yml:213`)** — the install line `npm install -g @openspec/cli` assumes the package exists under that scoped name. The real CLI ships from a different name (the local `openspec` binary on disk came from somewhere else). Verify the npm package exists and pin its version, or switch to invoking the CLI via `npx <package>@<version>`. Combined with Blocker 2, this currently disables the entire spec-validation gate.

2. **CI does not actually run the qsc-critic agent (`.github/workflows/ci.yml`)** — the file's leading comment says "The qsc-critic agent comment runs as a separate, non-blocking job (AI review is advisory, not gating). It is wired in `.github/workflows/critic.yml` so it can be disabled independently." That file does not exist. Tasks 5.1 promises "qsc-critic agent run on every pull request"; tasks 7.2 acknowledges this M1 review is pending. The README/proposal promise and the CI reality don't match.
   - Suggested fix: either add a `critic.yml` stub (even just one that invokes the Claude SDK with the agent definition file), or amend the comment in `ci.yml` to say "wired via local invocation only for M1; CI integration in M7."

3. **`dotnet test` runs Coverlet twice (`tests/QscDspDevices.UnitTests/QscDspDevices.UnitTests.csproj:11-14` + CI `--collect:"XPlat Code Coverage"`)** — the test csproj sets `<CollectCoverage>true</CollectCoverage>` which enables `coverlet.msbuild` integration during `dotnet test`, AND the CI line passes `--collect:"XPlat Code Coverage"` which enables `coverlet.collector`. Both packages are referenced. You'll get two coverage runs producing two outputs in different directories (`artifacts/coverage/{kind}/` from msbuild + `artifacts/test-results/{kind}/<guid>/coverage.cobertura.xml` from collector). The merge step uses the collector path, which works, but the msbuild-side artifacts are unused dead weight. Pick one mode and remove the other.

4. **`dotnet format --verify-no-changes --severity warn` (`.github/workflows/ci.yml:99`)** — `--severity warn` only reports issues at warning level or higher, so info-level style suggestions are silently allowed even though the .editorconfig declares many at suggestion level. If the goal is "PRs cannot merge dirty," drop the `--severity` argument and let the default behaviour catch everything the analyzer cares about.

5. **`BasicTcpClient` stub constructor does not validate hostname/port/buffer (`src/FrameworkStubs/CommonUtils/NetComs/BasicTcpClient.cs:13-21`)** — the framework docs explicitly list `ArgumentNullException` (null/empty hostname) and `ArgumentException` (port outside 0–65535, buffer < 0) as throwables. The stub silently stores any value. The framework-stubs spec permits "trivial property storage and constructor assignment" but explicitly invalidates "tests that pass on a hidden default." A production-side test that hands the transport an empty hostname will pass against the stub but throw against the real package.
   - Suggested fix: add the documented argument validation (it's three `if` statements). It's cheap and brings the stub up to the documented contract.

6. **`SPEC_COMPLIANCE.md` does not mention the `IAudioRoutable` namespace deviation** — README §3 lists `gcu_hardware_service.AudioDevices.IAudioRoutable`, but the framework docs and the stub put `IAudioRoutable` in `gcu_hardware_service.Routable`. Tasks 4.13 calls this out ("lives in Routable per docs"); the deviations section in `SPEC_COMPLIANCE.md` covers `IRedundancySupport` (D2) but not this one. Add a `D2.5` (or renumber D2 to cover both) so the reviewer sees it on first read.

7. **`HARDWARE_VALIDATION.md` cites requirement IDs (`R7.1`, `R8.7`, `R3.5`, `D3`, `R5.5`) that don't exist in `SPEC_COMPLIANCE.md`** — the matrix uses `7.1`, `8.7`, `3.5` etc. (no `R` prefix). Either prefix every spec-compliance row with `R` or drop the `R` from the validation checklist. Right now the cross-reference is broken.

8. **`tasks.md` line 4.2 lists Logger methods as "`Initialize`, `Enable/Disable/Destroy`"** — the framework docs name them `EnableDebug`/`DisableDebug`/`Destroy` and the stub correctly implements `EnableDebug`/`DisableDebug`. The task description is wrong; the code is right. Trivial fix, but worth correcting since the bet is partly about whether tasks-and-code stay in sync.

9. **Stub source-file count mismatch** — proposal.md says "32 framework-stub source files in `src/FrameworkStubs/` (857 lines)"; actual count is 29 `.cs` files. Some files (`GenericEventArgs.cs`, `LoggingTypes.cs`) hold multiple sibling types, so 32 might refer to *types*. Either way the proposal's literal claim is off by three. Update the count or restate as "29 files / 32 types."

10. **`openspec/changes/.../specs/framework-stubs/spec.md` says headers should read `// Source: framework-docs/...md`** but every actual stub uses `// Spec source: framework-docs/...md`. The intent is clearly the same; either align the spec wording with the chosen prefix, or s/Spec source/Source/ across the 29 files.

11. **Test coverage 90% threshold is fragile (`.github/workflows/ci.yml:147-153`)** — the parser does `grep -E '^\s*Line coverage:' artifacts/coverage-report/Summary.txt`. If `ReportGenerator` output changes one character (e.g. localised number format, or "Line coverage" → "Line covered"), the gate becomes "could not parse → warn → pass." Add a pinned `--reporttypes` schema check or use an actual exit-code-driven threshold tool (e.g. `coverlet --threshold` via a dedicated pass).

12. **`.editorconfig` defines `SA1101` twice (lines 95 and 109)** — last write wins, both set to `none`, no functional impact. Cosmetic only — clean up so the rule appears once.

13. **`Directory.Build.props` `<NoWarn>$(NoWarn)</NoWarn>` (line 34)** — this is a no-op (sets NoWarn to its current value) and gives the misleading impression that suppressions are deliberate. Either remove the line or list the suppressions explicitly.

---

## Nits (style, doc tweaks)

- `BasicTcpClient.Dispose(bool)` references each event with `_ = ConnectionFailed;` to suppress CS0067 (`src/FrameworkStubs/CommonUtils/NetComs/BasicTcpClient.cs:74-78`). CS0067 is already in the project's `<NoWarn>` list, so the discards are redundant. Either remove them or remove CS0067 from `<NoWarn>` and lean on the discards alone — pick one mechanism.
- `FrameworkStubs.csproj` `<NoWarn>` (line 77) lists `SA1101` and `CA1716` twice each. No functional impact; tighten for tidiness.
- `FrameworkStubs.csproj` `<NoWarn>` lists 50+ rule IDs on a single line. Consider splitting one-per-line with the existing rationale block — the current 600-character single-line list is unreadable in a diff.
- `Connection.cs` uses `using gcu_domain_service.Data;` to import `BaseData` (`src/FrameworkStubs/DomainService/ConnectionData/Connection.cs:6`) but this is implicit because `gcu_domain_service.Data.ConnectionData` already nests under `gcu_domain_service.Data`. The import is harmless but unnecessary — and it's the kind of microscopic finding the reviewer will use to test attention to detail. Same pattern in `Authentication.cs`, `Channel.cs`, `Dsp.cs`, `LogicTrigger.cs`, `Preset.cs`.
- `tests/Directory.Build.props` line 25 declares `CA2007` is suppressed because "irrelevant in xUnit context" — true, but `CA2007` only applies to projects with `async`/`await`, which the public surface forbids. The suppression is a pre-emptive future-proofing; consider noting that explicitly or removing it until needed.
- `ARCHITECTURE.md` "Concurrency policy" promises `Volatile.Read`/`Volatile.Write` for `IsOnline` (line 116-117) — `BaseDevice.IsOnline` in the stub is a plain auto-property. M2 will need to either override with a backing field or accept that the framework's setter cannot be made volatile. Note this in the M2 design doc.
- `PULL_REQUEST_TEMPLATE.md` "Run the agent locally with `claude /agents qsc-critic` before pushing." (line 30) — the actual Claude Code invocation is `claude` followed by a prompt; there is no `/agents` slash command. Replace with the documented invocation (or the SDK call) once Concern 2 is resolved.

---

## Praise

- Every stub source file leads with a precise pointer to its spec source (`// Spec source: framework-docs/.../X.md`), and the production-vs-stub-vs-real-package distinction is repeated inline. This is the audit trail the reviewer will appreciate.
- The `SPEC_COMPLIANCE.md` deviations cite real, fetchable QSC URLs (`help.qsys.com`, `q-syshelp.qsc.com`) — concrete sources beat hand-waving every time.
- The `Directory.Build.props` strict-mode set (`TreatWarningsAsErrors`, `WarningLevel=9999`, `AnalysisLevel=latest-all`, `EnforceCodeStyleInBuild`, plus three analyzer packages) plus per-project `<GenerateDocumentationFile>true</GenerateDocumentationFile>` is exactly the setup that makes "0 warnings 0 errors" a meaningful claim instead of an assertion.

---

## What I did NOT verify

- I did not actually fetch the cited QSC URLs to confirm they resolve and contain the quoted strings. They look authentic, but the reviewer will check.
- I did not run `dotnet test` (no tests exist yet for this milestone — confirmed by the task list).
- I did not run `dotnet format --verify-no-changes` locally.
- I did not run mutation testing (Stryker not configured until M7).
- I did not test the swap procedure in `src/FrameworkStubs/README.md` against the real GCU NuGet packages (binaries are not in the repo).
- I did not exhaustively diff every framework-docs `.md` against every stub line-by-line — I spot-checked ~12 of 29 stubs and the public API matches in those.
- I did not confirm whether `npm i -g @openspec/cli` actually exists; I noted the risk in Concern 1 but did not attempt the install in CI.
- I did not verify the Crestron SDK CS0162 warning actually emits in the build output and is suppressed by the narrow `<NoWarn>` (no easy way to do this without intentionally introducing unreachable code in production source, which would violate the read-only review rule).
