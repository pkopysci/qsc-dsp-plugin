# Spec Compliance Matrix — QscDspDevices

> **What this is.** A line-by-line audit trail mapping every requirement
> bullet from the project `README.md` to (a) the source file/line that
> implements it and (b) the test that verifies it. Filled out as
> milestones land. Empty rows mean "not yet implemented" and are tracked
> in the corresponding OpenSpec proposal.
>
> **How to read it.** "Implementation" cites the canonical place the
> behaviour lives; ancillary touches are not listed. "Test" cites the
> test that would fail first if the behaviour regressed. "Status"
> values: ✅ implemented & tested, ⚠ partial, ⏳ planned for milestone N,
> ❌ deviation (see Deviations section below).

## 1. Project structure & names (README §1)

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 1.1 | Library named `QscDspDevices` | `src/QscDspDevices/QscDspDevices.csproj:8` (`<AssemblyName>QscDspDevices</AssemblyName>`) | Build artefact name verified in CI (`.github/workflows/ci.yml`) | ✅ |
| 1.2 | Root namespace `QscDspDevices` | `src/QscDspDevices/QscDspDevices.csproj:9` | Compile-time | ✅ |
| 1.3 | Root public class `QscDspTcp` | `src/QscDspDevices/Plugin/QscDspTcp.cs` | _planned M2_ | ⏳ M2 |
| 1.4 | Microsoft Conventions everywhere else | `.editorconfig`, `Directory.Build.props` (`AnalysisLevel=latest-all`) + StyleCop.Analyzers + Roslynator | `dotnet format --verify-no-changes` in CI | ✅ |

## 2. Documentation & logging (README §2)

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 2.1 | All errors/warnings/debug via `gcu_common_utils.Logging.Logger` | _all `Logger.*` callsites_ | qsc-critic finding "no Console/Trace direct logging" | ⏳ M2 |
| 2.2 | XML doc comments on every public/protected member | `Directory.Build.props` (`GenerateDocumentationFile=true` + `TreatWarningsAsErrors=true` + analyser suite) | Compile-time `CS1591` failures | ✅ (enforced) |

## 3. API implementation (README §3)

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 3.1 | Use QRC (and ECP when appropriate) | `Backends/QrcBackend.cs`, `Backends/EcpBackend.cs` | _planned M2 / M-ECP_ | ⏳ M2 + M-ECP |
| 3.2 | Implement `BaseDevice` (Manufacturer=`QSC`, Model=`Q-SYS Core`) | `Plugin/QscDspTcp.cs` ctor | _planned M2_ | ⏳ M2 |
| 3.3 | Override `Connect()`/`Disconnect()` to manage TCP/IP | `Plugin/QscDspTcp.cs`, `Connectivity/ConnectionManager.cs` | _planned M2_ | ⏳ M2 |
| 3.4 | Set `IsOnline` then call `NotifyOnlineStatus()` | `Connectivity/ConnectionManager.cs` | _planned M2_ | ⏳ M2 |
| 3.5 | Implement `IDsp`, `IAudioRoutable`, `IAudioZoneEnabler`, `IRedundancySupport` (typo `IRedundanceSupport`), `IDspLogicTriggerSupport` | various | various | ⏳ M3–M6 |

## 4. Restrictions (README §4)

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 4.1 | Internal thread count ≤ 3 | `Plugin/QscDspTcp.cs` allocates send / receive / timer threads only | `tests/.../ThreadCensusTests.cs` | ⏳ M2 |
| 4.2 | No public async/await | architectural — enforced by qsc-critic check `R5` | qsc-critic agent on every PR | ⏳ ongoing |
| 4.3 | Use `gcu_common_utils.NetComs.BasicTcpClient` | `Transport/BasicTcpClientTransport.cs` | _planned M2_ | ⏳ M2 |
| 4.4 | IDisposable everywhere with the standard pattern | each owning class | unit tests for double-Dispose, Dispose-then-call | ⏳ ongoing |
| 4.5 | Compile zero warnings (Crestron warnings excepted) | `Directory.Build.props` `TreatWarningsAsErrors=true` + narrow `<NoWarn>CS0162</NoWarn>` on Crestron-using projects, with comment citing README §4 | CI build job | ✅ |
| 4.6 | Release DLL ≤ 500 KB | _measurement only_ | CI `Verify DLL size budget` step | ✅ (enforced) |

## 5. Scope of work — basic features (README §5)

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 5.1 | Zero–many audio presets | `Domain/PresetRegistry.cs` | _planned M3_ | ⏳ M3 |
| 5.2 | Zero–many audio input channels | `Domain/InputChannelRegistry.cs` | _planned M3_ | ⏳ M3 |
| 5.3 | Zero–many audio output channels | `Domain/OutputChannelRegistry.cs` | _planned M3_ | ⏳ M3 |
| 5.4 | Matrix routing for all output channels | `Domain/MatrixRouter.cs` | _planned M4_ | ⏳ M4 |
| 5.5 | Zero–one redundant Q-SYS Core, fail-over and switch-back | `Connectivity/RedundantCorePair.cs` | _planned M6_ | ⏳ M6 |

## 6. Q-SYS specific component support (README §5)

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 6.1 | Named Controls for routing/gain/mute/triggers | `Backends/QrcBackend.cs` (`Control.Get` / `Control.Set`) | _planned M2/M3_ | ⏳ M2/M3 |
| 6.2 | Numeric channel/mixer gain | `Domain/GainScaler.cs` (0–100 ↔ native) | property tests | ⏳ M3 |
| 6.3 | Boolean channel/mixer mute | `Backends/QrcBackend.cs` | unit tests | ⏳ M3 |
| 6.4 | Numeric matrix-router controls | `Domain/MatrixRouter.cs` | _planned M4_ | ⏳ M4 |
| 6.5 | Numeric/bool/string named controls associated with gain/mute/router | `Backends/QrcBackend.cs` | unit tests | ⏳ M3/M4 |
| 6.6 | Snapshots for `IAudioControl.RecallAudioPreset()` and `AddAudioPreset()` | `Backends/QrcBackend.cs` (`Snapshot.Load`) | _planned M3_ | ⏳ M3 |

## 7. Device connection (README §"Device Connection")

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 7.1 | Maintain connection until external `Disconnect()` | `Connectivity/ConnectionManager.cs` | integration test | ⏳ M2 |
| 7.2 | Lost connection → reconnect immediately if no backup, else switch to backup | `Connectivity/RedundantCorePair.cs` | integration test | ⏳ M6 |
| 7.3 | On failed connection: log error, wait 15s, retry until external Disconnect() | `Connectivity/ReconnectStrategy.cs` | integration test (deterministic clock) | ⏳ M2 |
| 7.4 | On any disconnect: update `IsOnline`, then call `NotifyOnlineStatus()` | `Connectivity/ConnectionManager.cs` | unit test | ⏳ M2 |
| 7.5 | On successful connection: hydrate state of channels/routing/logic/snapshots | `Connectivity/StateHydrator.cs` | integration test (fake QRC server) | ⏳ M3 |

## 8. Sending/receiving (README §"Sending/Receiving")

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 8.1 | Commands sent ASAP | `Protocol/CommandQueue.cs` | unit test | ⏳ M2 |
| 8.2 | Queue while sending; send next at next opportunity | `Protocol/CommandQueue.cs` | unit test | ⏳ M2 |
| 8.3 | FIFO order | `Protocol/CommandQueue.cs` (uses `ConcurrentQueue<T>`) | property test (FsCheck) | ⏳ M2 |
| 8.4 | Refuse send/queue while disconnected; log error | `Protocol/CommandQueue.cs` | unit test | ⏳ M2 |
| 8.5 | Clear queue on any disconnect | `Protocol/CommandQueue.cs` | unit test | ⏳ M2 |
| 8.6 | Maintain up-to-date control state via polling/subscriptions | `Protocol/ChangeGroupSubscriber.cs` (QRC AutoPoll) | integration test | ⏳ M3 |
| 8.7 | On state update: update internal state THEN notify subscribers | `Domain/{In,Out}putChannel.cs` | unit test (event-after-mutation invariant) | ⏳ M3 |

## 9. Exception handling (README §"Exception Handling")

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 9.1 | Avoid unhandled exceptions unless absolutely necessary | every public mutator wraps internal calls in a top-level try/catch + log | qsc-critic check; unit tests inject failures | ⏳ ongoing |
| 9.2 | Library must not crash the host | combination of 9.1 plus thread-level safety net in `Plugin/QscDspTcp.cs` | integration tests with fault injection | ⏳ ongoing |
| 9.3 | All exceptions/logic errors recorded as `Logger.Error` | every catch block | qsc-critic check | ⏳ ongoing |

## Deviations and justifications

The full design rationale lives in `openspec/project.md §"Known Spec
Deviations and Justifications"` and the per-milestone `design.md` files.
This section summarises them with explicit citations so the reviewer can
trace each one.

### D1. TLS support — README "TCP/IP connection" only

**README implication.** TLS would be the security best practice for a
real-world deployment.
**What QSC actually publishes.** QRC has no TLS port. Real-time control
communications are unencrypted as of Q-SYS 9.12.
**Citation.**
<https://help.qsys.com/q-sys_9.10/Content/Security/Scope_Protocols.htm>:
*"Real-time control communications between Cores and peripherals are not
currently encrypted but are planned to be added in an upcoming software
release."*
**Plugin behaviour.** Plaintext only on QRC. The transport layer
(`IConnectionTransport`) is designed so an `SslStream` can be wrapped in
when QSC ships TLS, with no API churn.

### D2. README typo `IRedundanceSupport` vs framework `IRedundancySupport`

**README §3.** Lists the interface as `IRedundanceSupport`.
**Framework docs.** The actual interface in
`framework-docs/gcu-hardware-service/IRedundancySupport.md` is named
`IRedundancySupport` (correctly spelled), and lives in namespace
`gcu_hardware_service.Redundancy`.
**Plugin behaviour.** Implements the correctly-spelled type. If the real
GCU NuGet exposes both as aliases, our implementation also satisfies the
alias.

### D2.5. README namespace `gcu_hardware_service.AudioDevices.IAudioRoutable` vs framework `gcu_hardware_service.Routable.IAudioRoutable`

**README §3.** Code-block lists the interface under the
`gcu_hardware_service.AudioDevices` namespace alongside `IDsp`,
`IAudioZoneEnabler`, etc.
**Framework docs.** `framework-docs/gcu-hardware-service/IAudioRoutable.md`
declares the interface in namespace `gcu_hardware_service.Routable` (a
sibling of `AudioDevices`, not a child).
**Plugin behaviour.** Implements `gcu_hardware_service.Routable.IAudioRoutable`
— the canonical, framework-documented namespace. The README's namespace
attribution is a documentation error. The plugin still satisfies "the
class implements `IAudioRoutable`" which is what the README §3 bullet
point literally requires.

### D3. Auto-switchback to primary

**README §"Device Connection".** *"the library must switch back to the
primary once it comes back online and is connected to a backup device."*
**QSC official guidance.** *"After a failover, Q-SYS does not
automatically change back to the failed Core when the Core recovers."*
(<https://q-syshelp.qsc.com/Content/Redundancy/Redundancy_Core.htm>.)
**Plugin behaviour.** Honours the README: when the original primary
returns to `Active` state, the plugin switches its writes back to the
primary. This is configurable via `RespectQscFailoverGuidance` (default
`false`); future operators who want QSC's recommended behaviour can opt
out.

### D4. ECP capability ceiling

**README §"Q-SYS Control Protocol".** *"This library must use QRC (and
ECP when appropriate)."*
**ECP capability limits per QSC docs.** ECP cannot enumerate components,
cannot address sub-controls (e.g. mixer crosspoints), and has no
atomic-multi-set. All control flows through Named Controls exposed in
the Q-SYS design.
**Plugin behaviour.** ECP is implemented as a feature-limited fallback
backend. When ECP is the active backend and an `IAudioControl` operation
cannot be expressed in ECP, the plugin logs a `Logger.Warn` and returns
the documented fallback (e.g. 0 for level queries on unsupported
controls). The Hardware Validation Checklist documents which methods
degrade.
