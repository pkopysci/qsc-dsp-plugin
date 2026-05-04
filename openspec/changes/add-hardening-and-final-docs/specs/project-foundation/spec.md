# Project Foundation — Spec Delta (M7)

## MODIFIED Requirements

### Requirement: Quality gates enforced in CI

The CI workflow SHALL fail the build when any of the following gates is violated:

- `dotnet build -c Release -warnaserror`: 0 warnings, 0 errors.
- `dotnet format --verify-no-changes`: zero diffs.
- `dotnet test`: zero failed tests.
- Merged Cobertura line coverage on `QscDspDevices.dll`: at least **91 %**. (M2 set the floor at 90 %; M7 raises it to 91 % to reflect the post-slice-1–5 baseline. Aspirational target ≥ 92 % deferred to M-ECP once the surface-reduction work happens — a smaller surface improves the ratio mechanically.)
- Release DLL size of `QscDspDevices.dll`: at most 500 KB.
- `openspec validate <change-id> --strict`: passes for any non-archived change in the diff.

#### Scenario: PR drops coverage below 91 %

- **GIVEN** a PR whose merged Cobertura line coverage is 90.9 %
- **WHEN** the coverage gate runs in CI
- **THEN** the workflow fails with a message naming the threshold

## ADDED Requirements

### Requirement: Public API surface is snapshot-locked

The repository SHALL include a snapshot of the public API surface of `QscDspDevices.dll` at `tests/QscDspDevices.UnitTests/PublicSurface.expected.txt`. Any change to a `public` symbol — addition, removal, signature change — MUST be reflected by a diff in that snapshot file in the same commit. A PR that changes the public surface without updating the snapshot MUST fail the test gate `PublicSurfaceTests.Public_surface_matches_expected_snapshot` (xunit, runs in the unit-test project under `dotnet test`).

#### Scenario: PR adds a public method without updating the snapshot

- **GIVEN** a PR that adds `public void DoNewThing()` to a class in `QscDspDevices.dll`
- **WHEN** `dotnet test` runs
- **THEN** `PublicSurfaceTests.Public_surface_matches_expected_snapshot` fails with a FluentAssertions diff naming the new symbol and pointing the author to update the expected file
