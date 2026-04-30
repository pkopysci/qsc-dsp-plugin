# Project Foundation — Spec Delta

## ADDED Requirements

### Requirement: Solution layout

The repository SHALL contain a single .NET 8 solution `QscDspDevices.sln`
at the repo root that aggregates exactly six projects: one shipped library
(`src/QscDspDevices`), one stub library (`src/FrameworkStubs`), and four
test projects (`tests/QscDspDevices.UnitTests`, `tests/QscDspDevices.IntegrationTests`,
`tests/QscDspDevices.PropertyTests`, `tests/QscDspDevices.TestSupport`).

#### Scenario: Fresh checkout, restore, build

- **WHEN** a developer runs `dotnet restore && dotnet build` from a clean clone
- **THEN** all six projects restore and compile successfully
- **AND** the build produces zero warnings and zero errors

#### Scenario: Solution file lists every project

- **WHEN** `dotnet sln list` is run at the repo root
- **THEN** the output contains all six expected `.csproj` paths

### Requirement: Strict compile configuration

The build SHALL enforce strict compile and analysis settings on every
project, with relaxations limited to test projects and explicitly justified
on a per-rule basis.

The shipped library project SHALL produce an XML documentation file and
SHALL fail the build if any public or protected member lacks XML doc
comments.

The build SHALL treat compiler warnings as errors except for the
specifically allowed Crestron SDK warning (CS0162 emitted by
`SimplSharpPostProcess`), which SHALL be suppressed only on projects that
reference the Crestron SDK with an inline comment citing README §4.

#### Scenario: Build fails on missing XML doc on shipped library member

- **GIVEN** a public method in `src/QscDspDevices/` without an XML doc comment
- **WHEN** `dotnet build` runs
- **THEN** the build fails with a `CS1591` (or analyzer-equivalent) error

#### Scenario: Build passes despite Crestron SDK auto-generated warning

- **WHEN** `dotnet build` runs against the unmodified solution
- **THEN** the Crestron `SimplSharpPostProcess` target emits its CS0162 warning internally
- **AND** the build still completes with zero unsuppressed warnings and zero errors

#### Scenario: Build fails on user-written unreachable code

- **GIVEN** unreachable code in `src/QscDspDevices/` written by a developer
- **WHEN** `dotnet build` runs
- **THEN** the build fails with `CS0162` because the suppression is scoped only to the post-process compilation, not to user code

### Requirement: Central package management

Every NuGet package version used by any project SHALL be pinned in a
single `Directory.Packages.props` at the repo root. Project files SHALL
reference packages without a `Version` attribute.

The list of allowed third-party NuGet packages SHALL be limited to those
permitted by README §"Allowed 3rd Party NuGet Packages":
`Newtonsoft.Json 13.0.3`, `Crestron.SimplSharp.SDK.ProgramLibrary 2.21.237`,
plus any package transitively required by the GCU framework packages.

#### Scenario: Reviewer audits dependency versions

- **WHEN** the reviewer reads `Directory.Packages.props`
- **THEN** every NuGet package and version used by the solution is visible in one file

#### Scenario: A project tries to add a non-whitelisted dependency

- **GIVEN** a developer adds `<PackageReference Include="SomeRandomLib"/>` to any csproj
- **AND** `SomeRandomLib` is not declared in `Directory.Packages.props`
- **WHEN** `dotnet restore` runs
- **THEN** restore fails with NU1010 (no PackageVersion entry)

### Requirement: EditorConfig and analyzer configuration

The repo root SHALL contain an `.editorconfig` enforcing Microsoft
Framework Design Guidelines naming, layout, and style rules. Every
explicit override of a default analyzer rule SHALL include an inline
comment giving the rationale for the override.

`StyleCop.Analyzers`, `Roslynator.Analyzers`, and
`Microsoft.CodeAnalysis.NetAnalyzers` SHALL be referenced from
`Directory.Build.props` so every project receives them automatically.

#### Scenario: dotnet format --verify-no-changes is clean

- **WHEN** `dotnet format --verify-no-changes` runs at the repo root
- **THEN** it exits with status 0 (no changes required)

### Requirement: Test infrastructure

Test projects SHALL live under `tests/` and never under `src/`. Tests
SHALL never be bundled into the shipped DLL.

A shared `tests/Directory.Build.props` SHALL relax XML-doc generation
and the specific test-time-only analyzer warnings (SA0001, SA1518,
CA1707, CA1515, CA2007) with inline justifications.

Coverage data SHALL be emitted by every test project via Coverlet,
output to `artifacts/coverage/{kind}/` where `{kind}` is one of
`unit`, `integration`, or `property`.

#### Scenario: Test project naming convention

- **WHEN** a new test project is added
- **THEN** its name is `QscDspDevices.{Kind}Tests` (e.g. `QscDspDevices.UnitTests`)
- **AND** its csproj sets `<IsTestProject>true</IsTestProject>`

#### Scenario: Coverage data is produced on every test run

- **WHEN** `dotnet test` runs
- **THEN** `artifacts/coverage/unit/coverage.cobertura.xml` (and matching files for the other test kinds) is produced
