# Tasks — add-project-foundation

## 1. Solution & projects

- [x] 1.1 Create `QscDspDevices.sln` at the repo root.
- [x] 1.2 Create `src/QscDspDevices/` class library (net8.0, AssemblyName=`QscDspDevices`, RootNamespace=`QscDspDevices`).
- [x] 1.3 Create `src/FrameworkStubs/` class library (net8.0, AssemblyName=`FrameworkStubs`).
- [x] 1.4 Create `tests/QscDspDevices.UnitTests/` (xUnit, Moq, FluentAssertions, Coverlet).
- [x] 1.5 Create `tests/QscDspDevices.IntegrationTests/` (xUnit, FluentAssertions, Coverlet).
- [x] 1.6 Create `tests/QscDspDevices.PropertyTests/` (xUnit, FsCheck, Coverlet).
- [x] 1.7 Create `tests/QscDspDevices.TestSupport/` (shared fixtures, fake servers).
- [x] 1.8 Add all six projects to the solution.

## 2. Build configuration

- [x] 2.1 Create root `Directory.Build.props` with `Nullable=enable`, `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, `Deterministic=true`, `GenerateDocumentationFile=true`.
- [x] 2.2 Create root `Directory.Packages.props` with Central Package Management; pin every allowed NuGet version per README.
- [x] 2.3 Create `tests/Directory.Build.props` relaxing XML-doc generation and noise-only test analyzer warnings (SA0001, SA1518, CA1707, CA1515, CA2007).
- [x] 2.4 Create `.editorconfig` enforcing Microsoft Framework Design Guidelines, naming conventions, and StyleCop rule overrides each justified by comment.
- [x] 2.5 Suppress Crestron SDK CS0162 narrowly on the two projects that include the SDK.

## 3. Verification

- [x] 3.1 `dotnet restore` succeeds for all six projects.
- [x] 3.2 `dotnet build` produces zero warnings, zero errors.
- [x] 3.3 Verified analyzers fire by intentionally violating a rule (StyleCop SA1518 + SA0001 caught on first build attempt).

## 4. Framework stubs source files

- [x] 4.1 Generate stub files for `gcu_common_utils.GenericEventArgs.*` (`GenericSingleEventArgs<T>`, `GenericDualEventArgs<T1,T2>`, `GenericTrippleEventArgs<T1,T2,T3>`).
- [x] 4.2 Generate stub files for `gcu_common_utils.Logging.*` (`Logger` static class with `Error/Warn/Notice/Debug` methods, `Initialize`, `EnableDebug`/`DisableDebug`/`Destroy`, `IsInitialized`; `LogServiceTypes`, `LogDeviceTypes` enums).
- [x] 4.3 Generate stub files for `gcu_common_utils.NetComs.BasicTcpClient` (events: `ConnectionFailed`, `ClientConnected`, `StatusChanged`, `RxReceived`, `RxBytesReceived`; properties; `Connect/Disconnect/Send/Dispose`).
- [x] 4.4 Generate stub files for `gcu_common_utils.NetComs.TcpClientWrapper` (init-only props).
- [x] 4.5 Generate stub files for `gcu_common_utils.Validation.ParameterValidator` — implement REAL behaviour (the methods are trivial and used by production code).
- [x] 4.6 Generate stub files for `gcu_common_utils.Validation.DataFormatter` — implement REAL behaviour.
- [x] 4.7 Generate stub files for `gcu_common_utils.DataObjects.ListBuffer<T>` and `Vector2D`.
- [x] 4.8 Generate stub files for `gcu_hardware_service.BaseDevice.IBaseDevice`, `BaseDevice` (abstract), `DeviceContainer<T>`.
- [x] 4.9 Generate stub files for `gcu_hardware_service.AudioDevices.*` (`IAudioControl`, `IDsp`, `IAudioRoutable`, `IAudioZoneEnabler`, `IDspLogicTriggerSupport`).
- [x] 4.10 Generate stub files for `gcu_hardware_service.Redundancy.IRedundancySupport`.
- [x] 4.11 Generate stub files for `gcu_hardware_service.Communication.ITcpDevice`.
- [x] 4.12 Generate stub files for `gcu_hardware_service.PowerControl.IPowerControllable`.
- [x] 4.13 Generate stub files for `gcu_hardware_service.Routable.IAudioRoutable` (cross-check; lives in Routable per docs).
- [x] 4.14 Generate minimal-surface stubs for `gcu_domain_service.Data.*` types referenced through interface signatures (`Connection`, `Authentication`, `ComSpec`, `Dsp`, `Channel`, `Preset`, `LogicTrigger`, `ZoneEnableToggle`, `BaseData`).
- [x] 4.15 Verify `dotnet build` still produces zero warnings, zero errors.
- [x] 4.16 Add a `FrameworkStubs/README.md` documenting the swap procedure (replace `<ProjectReference>` with `<PackageReference>`s, restore, rebuild).

## 5. CI and reviewer agent

- [x] 5.1 Add `.github/workflows/ci.yml` with: build (warnings-as-errors), `dotnet format --verify-no-changes`, `dotnet test` with coverage merge, ReportGenerator HTML upload, DLL-size budget check (fail >500 KB; comparison in bytes), `openspec validate` job (pinned `@fission-ai/openspec@1.3.1`), mutation-testing on workflow_dispatch / main only.
- [x] 5.2 Add `.claude/agents/qsc-critic.md` red-team review subagent prompt. Agent runs LOCALLY (Claude Code) before each PR; CI does not invoke it because the runner has no Claude API access.
- [x] 5.3 Add `.gitignore` entries for `bin/`, `obj/`, `artifacts/`, `.vs/`, `*.user`, `*.suo`, coverage outputs.
- [x] 5.4 Add `.github/PULL_REQUEST_TEMPLATE.md` requiring spec-id reference and compliance citation.

## 6. Documentation skeleton

- [x] 6.1 Create `SPEC_COMPLIANCE.md` with the full README requirement matrix, implementation/test columns marked ⏳ for future milestones, and the four documented Deviations (D1–D4) with citations.
- [x] 6.2 Create `ARCHITECTURE.md` (layer diagram, three-thread budget table, lock order, error model, "what we deliberately do NOT do").
- [x] 6.3 Create `HARDWARE_VALIDATION.md` — manual checklist with sign-off footer for delivery.

## 7. Validation

- [x] 7.1 `openspec validate add-project-foundation --strict` passes.
- [ ] 7.2 `qsc-critic` subagent reviews the foundation; findings logged. (Pending — to run after this milestone is committed.)
