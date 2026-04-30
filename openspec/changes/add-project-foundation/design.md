# Design — Project Foundation

## Context

The repository as delivered to us contains:

- A `README.md` written by the third-party reviewer that defines the contract
  (project name, namespaces, allowed dependencies, behavioural requirements,
  resource budgets).
- A `framework-docs/` tree describing the public API of three private NuGet
  packages (`gcu-common-utils 4.3.3`, `gcu-hardware-service 4.3.4`,
  `gcu-domain-service 4.2.3`) that the plugin must build against.
- A `dependencies/` tree containing only `.nuspec` metadata for those three
  packages — **the actual `.nupkg` binaries are absent.**
- A `LICENSE` (MIT).

We must produce a single .NET 8 class library DLL ≤ 500 KB with zero
warnings, full XML doc coverage on public/protected API, capped at three
internal threads, no public async/await, and demonstrably strong test
coverage. The entire codebase will be audited by the reviewer.

## Goals / Non-Goals

**Goals**

- A six-project solution that builds clean from a fresh checkout in <10 s.
- Strict-by-default compile: every analyzer warning is a build error, every
  rule deviation is documented inline with rationale.
- A `FrameworkStubs` assembly whose public surface is identical to the real
  GCU packages so the production code never knows whether it is linking
  against the stubs or the real DLLs.
- A test infrastructure that can grow to hold thousands of unit, integration,
  and property tests without restructuring.
- Coverage and DLL-size measurement wired into CI from day 1.

**Non-Goals**

- Behavioural correctness of the stub bodies. They throw
  `NotImplementedException` on purpose to make any accidental dependency
  on stub behaviour fail loudly during unit tests.
- A working QRC client, fake server, or ECP support — those are later
  milestones.
- Hardware-in-the-loop testing — out of scope; the fake servers will replace
  it.

## Decisions

### Decision: Use Central Package Management

**What.** Every NuGet version is pinned exactly once in
`Directory.Packages.props` at the repo root; project files reference packages
without a `Version` attribute.

**Why.** The reviewer can audit dependency versions in one file. Version
drift between projects is impossible. CI reproducibility is improved.

**Alternatives considered.** Per-project `<Version>` attributes (more
verbose, drift-prone), or a `packages.lock.json` per project (locks the
*resolution* but not the *declaration*).

### Decision: Build a `FrameworkStubs` assembly rather than commit fake DLLs

**What.** A separate C# project in `src/FrameworkStubs/` that emits three
namespaces (`gcu_common_utils.*`, `gcu_hardware_service.*`,
`gcu_domain_service.*`) matching the real packages. Production code
references this project; at delivery time the reference is replaced with
the real NuGet packages.

**Why.** Provable, auditable spec compliance. The stubs are derived
mechanically from `framework-docs/` so we cannot accidentally code against
behaviour we couldn't have known about. The public surface is identical
by construction (proven by reading the markdown side-by-side).

**Alternatives considered.**
- Commit the real `.nupkg` files we don't have. Not possible.
- Reverse-engineer the GCU packages. Risks IP issues and is intellectually
  dishonest given the bet's premise.
- Wait for the reviewer to provide the binaries. Blocks all work; defeats
  the purpose of the bet.

### Decision: Strictest possible analyzer + warning configuration

**What.** `TreatWarningsAsErrors=true`, `WarningLevel=9999`,
`AnalysisLevel=latest-all`, `EnforceCodeStyleInBuild=true`, plus
StyleCop.Analyzers and Roslynator. CI runs `dotnet format
--verify-no-changes`.

**Why.** The reviewer's claim is "AI-generated code is slop." A green build
with this configuration is a counter-claim with mechanical teeth.

**Trade-offs.** Higher up-front friction; rule overrides must be documented.
We accept this — every override gets a rationale comment in `.editorconfig`.

### Decision: Crestron SDK CS0162 warning suppressed narrowly

**What.** `<NoWarn>$(NoWarn);CS0162</NoWarn>` on `QscDspDevices.csproj` and
`FrameworkStubs.csproj` only.

**Why.** The Crestron SimplSharp SDK's `SimplSharpPostProcess` MSBuild
target compiles a temporary auto-generated assembly-attributes file that
contains an unreachable `return` statement (out of our control). The
README §4 explicitly allows this: *"there is a known warning from Crestron
libraries that is unavoidable but does not affect functionality."* We
suppress narrowly so any unreachable code WE write remains an error.

**Alternative considered.** Globally suppress CS0162 in the root
`Directory.Build.props`. Rejected — it would mask unreachable code we
write ourselves.

### Decision: Test-project relaxations centralised in `tests/Directory.Build.props`

**What.** A second `Directory.Build.props` under `tests/` that imports the
root one then relaxes `GenerateDocumentationFile`, suppresses `SA0001`,
`SA1518` (auto-generated `GlobalUsings.cs` lacks trailing newline),
`CA1707` (test method naming), and `CA1515` (xUnit requires public test
classes).

**Why.** Source-of-truth pinning of every relaxation in one place, all
justified by inline comments. Each individual test project stays minimal.

### Decision: Coverage output to `artifacts/coverage/{kind}/`

**What.** Each test project's `CoverletOutput` writes to
`artifacts/coverage/{unit|integration|property}/`. CI merges them into a
single Cobertura report and enforces a project-level threshold.

**Why.** Multiple test types contribute to coverage; merging gives a true
project-wide number. Per-test-type artifacts also support targeted
mutation testing later.

## Risks / Trade-offs

- **Stub drift from real DLLs.** If the stub signatures diverge from the
  real package public surface, integration will break. *Mitigation:* the
  stub source files cite `framework-docs/`-relative paths in their file
  headers; a Compile-time integration pass against the real DLLs is added
  to the delivery checklist.
- **Strict warnings can mask real bugs by being noisy.** *Mitigation:* the
  inline `.editorconfig` rationale comments give a future maintainer the
  context to relax a rule when it genuinely fights the design, instead of
  being intimidated by the configuration.
- **CI cost.** Free GitHub Actions tier handles this comfortably for a
  public repo (unlimited minutes). On a hypothetical private fork, the
  mutation-testing job would need to be moved to a scheduled run only.

## Migration Plan

Greenfield. No migration.

## Open Questions

None blocking M1. Items from `research/FRAMEWORK_API_SURFACE.md §10`
("Open Questions") are addressed inline in stub source comments and
revisited as features that exercise them are added.
