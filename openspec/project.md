# Project Context

## Purpose

`QscDspDevices` is a hardware-control plug-in for an existing AV-system framework
("AV Framework" / GCU AV Framework). It provides remote control of QSC Q-SYS
audio DSP cores using QSC's QRC (Q-SYS Remote Control) protocol, with a fallback
ECP (External Control Protocol) backend for legacy controllers.

The plug-in is loaded at runtime by the AV Framework's infrastructure service
(see `framework-docs/gcu-hardware-service/InfrastructureService.md`) and runs on
a Crestron RMC4 processor in production deployments.

## Tech Stack

- **Language:** C# 12 on .NET 8.0 (LTS)
- **Library type:** Class library (single shipped DLL ≤ 500 KB)
- **Runtime target:** Crestron RMC4 (CrestronOS, AppDomain-restricted .NET)
- **Required framework dependencies (private GCU NuGet feed):**
  - `gcu-common-utils 4.3.3`
  - `gcu-hardware-service 4.3.4`
  - `gcu-domain-service 4.2.3`
- **Allowed third-party NuGet:**
  - `Newtonsoft.Json 13.0.3`
  - `Crestron.SimplSharp.SDK.ProgramLibrary 2.21.237`
  - Plus any package transitively required by the GCU framework packages.

For development we build against an in-repo `FrameworkStubs` assembly that
mirrors the public API of the GCU packages (the real `.nupkg` binaries are not
included in the repo, only nuspec metadata). At delivery time the stubs are
swapped for the real packages with no source change.

## Project Conventions

### Code Style

- **Language target:** C# 12, `<LangVersion>latest</LangVersion>`.
- **Nullable reference types:** enabled (`<Nullable>enable</Nullable>`).
- **Warnings as errors:** every warning fails the build
  (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`). The known harmless
  warnings emitted by the Crestron SDK are suppressed by ID in
  `Directory.Build.props` with a comment citing the SDK source.
- **Naming and design:** strict adherence to Microsoft Framework Design
  Guidelines (<https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/>).
  Deviations are only permitted where a whitelisted dependency forces them
  (e.g. `gcu_common_utils` snake-case namespace).
- **Formatter:** `dotnet format` with the repo's `.editorconfig`. CI runs
  `dotnet format --verify-no-changes`; PRs cannot merge dirty.
- **Analyzers:** `StyleCop.Analyzers` and `Roslynator.Analyzers` enabled
  project-wide. Rule overrides live in `.editorconfig` with rationale comments.
- **XML doc comments:** required on every `public` and `protected` member,
  with `<summary>`, `<param>`, `<returns>`, and `<exception>` where applicable.
  Documentation generation is enabled per project (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`).
- **Comments inside method bodies:** only when they explain *why*. Never restate
  what the code does.
- **No public `async`/`await`.** All threading is internal. Public surface is
  synchronous as required by the framework specification.

### Architecture Patterns

- **Single shipped assembly.** `src/QscDspDevices/QscDspDevices.csproj`
  produces one DLL named `QscDspDevices.dll`. No shared code is split into
  separate assemblies, to stay under the 500 KB budget.
- **Layered design inside the assembly:**
  1. `Transport` — `IConnectionTransport`, `BasicTcpClientTransport`,
     framing (null-byte for QRC, line-based for ECP).
  2. `Protocol` — JSON-RPC 2.0 framer, request/response correlation, AutoPoll
     subscription map (QRC); ASCII command formatter and parser (ECP).
  3. `Domain` — channel/preset/zone/router state, gain scaling
     (0–100 ↔ device-native), event publication.
  4. `Connectivity` — connection-manager state machine, 15-second reconnect
     loop, primary/backup failover.
  5. `Plugin` — the public `QscDspTcp` class wiring the layers and exposing
     the framework interfaces.
- **Threading budget: 3 internal threads maximum** (hard requirement).
  - Thread 1: outbound send loop (consumes the FIFO command queue).
  - Thread 2: inbound receive/dispatch loop (frames + deserializes + raises).
  - Thread 3: timer thread (keepalive `NoOp`, reconnect backoff, AutoPoll
    sanity).
  - All other work runs on the framework's calling thread or piggy-backs on
    these three. Verified at run-time by an internal `ThreadCensus` guard.
- **No `Task`/`async` on public API.** Internally we may use private
  `Task` methods but every public method blocks or returns immediately.
  Long-running work is queued and confirmed via events.
- **IDisposable everywhere a class owns resources.** Standard
  Dispose(bool disposing) pattern; finalizer only when truly required.
- **Dependency injection by constructor.** No static state outside the
  framework's `Logger`. Internal singletons are instance-per-plugin.
- **Fail-soft, log-loud.** Per the README: never crash the host. Every error
  path logs to `Logger.Error` (or `Warn`/`Notice`) and returns the documented
  fallback value (e.g. 0 for level queries on unknown ids).

### Testing Strategy

- **Tests live outside the shipped assembly** in `tests/`, never bundled.
- **Unit tests:** xUnit + Moq. Target ≥ 90 % line coverage on
  `QscDspDevices.dll`.
- **Property-based tests:** FsCheck for protocol framing, gain scaling
  (0–100 ↔ native), and queue invariants. Reason: small, well-defined math
  domains where fuzz testing finds edge cases unit tests miss.
- **Mutation testing:** `Stryker.NET` configured per project. Surviving
  mutants are tracked in `tests/MUTATION_REPORT.md`.
- **Integration tests:** run against an in-process fake QRC server and a
  fake ECP server (in `tests/Fakes/`) to exercise reconnect, queueing,
  failover, change-group sync, and authentication flows deterministically.
- **No hardware-in-the-loop required for CI.** The fake servers are
  spec-faithful; a separate hardware-validation checklist (`HARDWARE_VALIDATION.md`)
  documents how to exercise the plugin against a real Q-SYS Core or the
  Q-SYS Designer emulator.
- **CI gates:** build, test, format-check, coverage threshold, DLL-size
  check, and `qsc-critic` agent run on every pull request.

### Git Workflow

- **Default branch:** `main`. Always green.
- **Feature branches:** `milestone/m{n}-{short-name}`, e.g.
  `milestone/m2-qrc-client`. One milestone per PR.
- **Conventional commits** with imperative subject:
  `feat(transport): add null-byte framer`, `fix(reconnect): clear queue on drop`.
- **PR template:** every PR cites the OpenSpec change ID it implements,
  attaches the latest `qsc-critic` review excerpt, and links to the spec
  compliance line(s) it satisfies.
- **Merges:** squash-and-merge after CI green, owner approves. No direct
  pushes to `main`.

## Domain Context

- **Q-SYS Core** is QSC's family of audio DSP processors. The plugin treats
  it as an opaque device addressed by hostname/port and controlled via QRC
  (TCP/1710 plaintext JSON-RPC 2.0, null-byte framed) or ECP (TCP/1702 ASCII).
- **Named Controls** are QSC's way of exposing internal DSP elements
  (gain, mute, router, snapshots, logic triggers) to external clients by name.
  Configuration tells the plugin which Named Control corresponds to each
  channel/preset/router-output.
- **Change Groups** are QSC's subscription mechanism — the plugin asks the
  Core to push state changes for a list of controls at a specified poll rate.
  Limit: **4 change groups per connection.** We reserve 3 for our own use,
  leaving 1 for ad-hoc consumer queries.
- **Snapshots** are QSC's preset mechanism (`Snapshot.Load`/`Snapshot.Save`,
  bank + index addressing).
- **Redundant Cores:** Q-SYS supports primary+backup pairs. Detection is via
  `EngineStatus` push; `-32604` is the "you connected to a Standby Core"
  error code.

## Important Constraints

| Constraint | Source | Hard limit |
|------------|--------|-----------|
| Project name | README §1 | `QscDspDevices` |
| Root namespace | README §1 | `QscDspDevices` |
| Root public class | README §1 | `QscDspTcp` |
| Target framework | README env | `net8.0` (Class Library) |
| Internal thread count | README §4 | ≤ 3 concurrent |
| Public async/await | README §4 | **forbidden** |
| TCP client | README §4 | must use `gcu_common_utils.NetComs.BasicTcpClient` |
| Logger | README §2 | must use `gcu_common_utils.Logging.Logger` |
| XML doc comments | README §2 | required on every public/protected member |
| Compile output | README §4 | zero warnings (Crestron-SDK warnings excepted) |
| DLL size | README §4 | ≤ 500 KB |
| Reconnect backoff | README §"Device Connection" | 15 seconds, repeat until `Disconnect()` |
| Failover behaviour | README §"Device Connection" | switch back to primary when it returns (deviates from QSC's official guidance — see `SPEC_COMPLIANCE.md`) |
| Command queue policy | README §"Sending/Receiving" | FIFO, cleared on disconnect, refused while disconnected |

## External Dependencies

- **Q-SYS Core** (the device under control). Documentation:
  <https://help.qsys.com/q-sys_9.8/Content/External_Control_APIs/QRC/QRC_Overview.htm>
- **Q-SYS Designer Emulator** (a software emulator that listens on the same
  port 1710 — useful for development and manual testing without a real Core).
- **GCU AV Framework packages** (private feed; not bundled). At delivery time
  these replace the in-repo `FrameworkStubs` assembly.
- **Crestron SimplSharp SDK** (`Crestron.SimplSharp.SDK.ProgramLibrary 2.21.237`)
  — public NuGet, used for `SocketStatus` and `CrestronControlSystem` types
  referenced by the GCU framework.

## Known Spec Deviations and Justifications

These are documented in `SPEC_COMPLIANCE.md`. They are NOT silent deviations;
each is called out so the reviewer can evaluate them.

1. **TLS:** the README says only "TCP/IP connection". QRC has no TLS port
   in QSC's published protocol, so the plugin is plaintext-only on QRC.
   `IConnectionTransport` is designed for future TLS pluggability.
2. **README typo `IRedundanceSupport`:** the canonical interface name in
   the framework docs is `IRedundancySupport`. The plugin implements the
   correctly-spelled type.
3. **Auto-switchback to primary:** README requires it; QSC's official
   redundancy guidance says they don't auto-switchback. The plugin honours
   the README (it's the contract) and exposes a configurable opt-out for
   future operators who want QSC-recommended behaviour.
4. **ECP capability ceiling:** ECP cannot enumerate components, address
   sub-controls, or atomic-multi-set. The plugin implements ECP as a
   feature-limited fallback and documents which IAudioControl methods
   degrade or no-op when ECP is the active backend.
