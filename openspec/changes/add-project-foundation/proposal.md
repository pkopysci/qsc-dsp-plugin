# Change: Establish project foundation, build pipeline, and framework stubs

## Why

The repository starts as essentially empty (README, LICENSE, dependency
metadata, framework docs). Before any production code is written we need
a solid, audit-friendly foundation: solution layout, strict build configuration,
analyzer enforcement, central package management, and a stand-in for the
private GCU framework binaries that are not in the repo.

The bet that motivates this work hinges on visible rigor — every PR must
demonstrate that the codebase enforces "clean compile, zero warning, zero
style violation, full XML doc coverage" mechanically, not by promise. Doing
the foundation right means every subsequent milestone PR is a small step on
solid ground rather than a heroic effort to fix many things at once.

## What Changes

- Add a .NET 8 solution `QscDspDevices.sln` with six projects:
  - `src/QscDspDevices/` — the deliverable class library (single shipped DLL).
  - `src/FrameworkStubs/` — spec-faithful stand-in for the GCU AV Framework
    NuGet packages (`gcu-common-utils 4.3.3`, `gcu-hardware-service 4.3.4`,
    `gcu-domain-service 4.2.3`).
  - `tests/QscDspDevices.UnitTests/` — xUnit + Moq + FluentAssertions.
  - `tests/QscDspDevices.IntegrationTests/` — xUnit, runs against the
    in-process fake QRC/ECP servers (added in M2/M-ECP).
  - `tests/QscDspDevices.PropertyTests/` — FsCheck for property-based tests.
  - `tests/QscDspDevices.TestSupport/` — shared fixtures, fake servers,
    deterministic clock.
- Add `Directory.Build.props` (root) — `Nullable=enable`,
  `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, deterministic
  builds, doc-xml generation, repo-wide analyzer references
  (StyleCop.Analyzers, Roslynator, Microsoft.CodeAnalysis.NetAnalyzers).
- Add `Directory.Packages.props` (root) — Central Package Management; every
  NuGet version pinned in one file. Allowed third-party deps per README:
  `Newtonsoft.Json 13.0.3`, `Crestron.SimplSharp.SDK.ProgramLibrary 2.21.237`.
- Add `tests/Directory.Build.props` — relaxes XML-doc requirements for tests,
  configures Coverlet output to `artifacts/coverage/{kind}/`.
- Add `.editorconfig` — full Microsoft Framework Design Guidelines style,
  StyleCop rule overrides justified by comment, naming conventions enforced
  as warnings/errors.
- Add `FrameworkStubs` source files mirroring the documented public API of
  the three GCU framework packages exactly, namespace-by-namespace, copied
  verbatim from `framework-docs/` — 29 `.cs` files holding 32 types, 880 lines.
  **Bodies throw `NotImplementedException`** on every non-trivial member;
  only auto-property storage, constructor assignment, and members whose
  documented behaviour is genuinely trivial (`Vector2D.Equals`, `Equals`'s
  hashing companion, `ParameterValidator.*`, `DataFormatter.*`) carry a
  real implementation.
- Add `.github/workflows/ci.yml` — build + test + format-verify + size
  budget + coverage threshold + `qsc-critic` agent run on every PR.
- Add `.claude/agents/qsc-critic.md` — red-team review subagent prompt.
- Add `.gitignore` entries for `bin/`, `obj/`, `artifacts/`,
  `.vs/`, `*.user`.

## Impact

- Affected specs: NEW capability `project-foundation`, NEW capability
  `framework-stubs`. No existing specs.
- Affected code: entire repository structure created from scratch; only
  `framework-docs/`, `dependencies/`, `LICENSE`, and `README.md` are
  pre-existing and untouched.
- Affected reviewers: anyone running `dotnet build` or `dotnet test`
  immediately gets the strict feedback loop.

## Out of scope (handled by later milestones)

- `QscDspTcp` public class skeleton — added in M2 with the connection
  manager.
- Fake QRC server — added in M2.
- Fake ECP server — added in M-ECP.
- Spec compliance matrix — partially populated in M1 with build-system
  mappings; fleshed out as features land.
