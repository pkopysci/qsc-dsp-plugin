# framework-stubs Specification

## Purpose
TBD - created by archiving change add-project-foundation. Update Purpose after archive.
## Requirements
### Requirement: Stub assembly mirrors documented public API exactly

The `FrameworkStubs` assembly SHALL expose every public type, member,
namespace, and inheritance edge documented in `framework-docs/` for the
three GCU packages (`gcu-common-utils`, `gcu-hardware-service`,
`gcu-domain-service`) verbatim.

Type names, member signatures, parameter names, default values, generic
constraints, return types, accessibility modifiers, and event-args
delegate types SHALL match the documentation exactly.

#### Scenario: Production code references `gcu_hardware_service.AudioDevices.IDsp.Initialize`

- **GIVEN** the documented signature is `Initialize(string hostId, int coreId, string hostname, int port, string username, string password)`
- **WHEN** production code calls `Initialize(...)` against the stub assembly
- **THEN** the call compiles
- **AND** when the stub is replaced with the real GCU NuGet package the same call still compiles unchanged

#### Scenario: Stub method is invoked at runtime

- **GIVEN** production code calls a non-trivial stub method (e.g. `BasicTcpClient.Connect()`)
- **WHEN** the call executes
- **THEN** a `NotImplementedException` is thrown with a message identifying the stubbed member

### Requirement: Stub members SHALL throw NotImplementedException for non-trivial bodies

Every stub method, property setter, and property getter that would require real behaviour MUST throw `NotImplementedException` with a diagnostic message identifying the stubbed member.

The only permitted non-throwing bodies are: auto-property storage, constructor parameter assignment to backing fields, trivial `string.Empty`/`default(T)` returns where the documentation explicitly defines a default, and the genuinely-trivial implementations of `ParameterValidator` and `DataFormatter` whose entire spec is exposed in `framework-docs/`.

#### Scenario: Test that depends on stub behaviour fails loudly

- **GIVEN** a test that assumes a stubbed member returns meaningful state
- **WHEN** the test runs against the stub assembly
- **THEN** the test fails with `NotImplementedException` rather than passing on a hidden default

### Requirement: Stub assembly never ships in the deliverable

The `FrameworkStubs` project SHALL set `<IsPackable>false</IsPackable>`.
The shipped deliverable (`QscDspDevices.dll`) SHALL not transitively
expose `FrameworkStubs.dll` to its consumers.

#### Scenario: Verify ship artifact excludes stub assembly

- **WHEN** the QscDspDevices project is packed (`dotnet pack`)
- **THEN** the resulting `.nupkg` contains `QscDspDevices.dll` only
- **AND** `FrameworkStubs.dll` is absent from `lib/net8.0/`

### Requirement: Stubs are swappable for the real packages with no source change

A documented procedure SHALL exist (`src/FrameworkStubs/README.md`) that
describes replacing the `<ProjectReference Include="..\FrameworkStubs\FrameworkStubs.csproj"/>`
in `src/QscDspDevices/QscDspDevices.csproj` with `<PackageReference>` lines
for the three real GCU packages, then re-restoring.

After the swap, the production source files SHALL compile without any
edit, because the public API surface is identical by construction.

#### Scenario: Reviewer performs the swap

- **GIVEN** the reviewer follows the swap procedure
- **WHEN** they restore and rebuild
- **THEN** `QscDspDevices.dll` compiles successfully against the real GCU packages with zero changes to its source files

### Requirement: Stub source files cite their docs source

Each stub source file SHALL begin with a header comment giving the
relative path to the `framework-docs/` markdown file from which its
public surface was transcribed. This SHALL form an audit trail proving
the stubs were derived from public documentation, not from peeked
private DLLs.

#### Scenario: Auditor traces a stub member to its documentation

- **GIVEN** a stub file `IDsp.cs`
- **WHEN** the auditor opens it
- **THEN** the file header reads e.g. `// Spec source: framework-docs/gcu-hardware-service/IDsp.md`

