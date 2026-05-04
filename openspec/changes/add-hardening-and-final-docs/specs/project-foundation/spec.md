# Project Foundation — Spec Delta (M7)

## MODIFIED Requirements

### Requirement: Quality gates enforced in CI

The CI workflow SHALL fail the build when any of the following gates is violated:

- `dotnet build -c Release -warnaserror`: 0 warnings, 0 errors.
- `dotnet format --verify-no-changes`: zero diffs.
- `dotnet test`: zero failed tests.
- Merged Cobertura line coverage on `QscDspDevices.dll`: at least **92 %**. (M2 set the floor at 90 %; M7 raises it to 92 % once the M3-deferred and M6-deferred tests land.)
- Release DLL size of `QscDspDevices.dll`: at most 500 KB.
- `openspec validate <change-id> --strict`: passes for any non-archived change in the diff.

#### Scenario: PR drops coverage below 92 %

- **GIVEN** a PR whose merged Cobertura line coverage is 91.9 %
- **WHEN** the coverage gate runs in CI
- **THEN** the workflow fails with a message naming the threshold

## ADDED Requirements

### Requirement: Public API surface is snapshot-locked

The build SHALL include a snapshot of the public API surface of `QscDspDevices.dll`. Any change to a `public` symbol — addition, removal, signature change — MUST be reflected by a diff in either `PublicAPI.Shipped.txt` (for shipped surface) or `PublicAPI.Unshipped.txt` (for additions in flight). A PR that changes the public surface without updating these files MUST fail the build via `Microsoft.CodeAnalysis.PublicApiAnalyzers` (RS0016/RS0017) or an equivalent analyzer.

#### Scenario: PR adds a public method without updating the snapshot

- **GIVEN** a PR that adds `public void DoNewThing()` to a class in `QscDspDevices.dll`
- **WHEN** `dotnet build` runs
- **THEN** the build fails with RS0016 (or equivalent) naming the new symbol
