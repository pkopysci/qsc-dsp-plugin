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
| 1.3 | Root public class `QscDspTcp` | `src/QscDspDevices/Plugin/QscDspTcp.cs` | `tests/QscDspDevices.UnitTests/Plugin/QscDspTcpTests.cs` (13 tests) | ✅ |
| 1.4 | Microsoft Conventions everywhere else | `.editorconfig`, `Directory.Build.props` (`AnalysisLevel=latest-all`) + StyleCop.Analyzers + Roslynator | `dotnet format --verify-no-changes` in CI | ✅ |

## 2. Documentation & logging (README §2)

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 2.1 | All errors/warnings/debug via `gcu_common_utils.Logging.Logger` | `src/QscDspDevices/Plugin/Log.cs` thin wrapper; every error path in `CommandQueue`, `JsonRpcDispatcher`, `ConnectionManager`, `QscDspTcp` routes through it | `tests/QscDspDevices.UnitTests/Protocol/CommandQueueTests` and `JsonRpcDispatcherTests` use `TestLoggerSink` to assert on log output | ✅ |
| 2.2 | XML doc comments on every public/protected member | `Directory.Build.props` (`GenerateDocumentationFile=true` + `TreatWarningsAsErrors=true` + analyser suite) | Compile-time `CS1591` failures | ✅ (enforced) |

## 3. API implementation (README §3)

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 3.1 | Use QRC (and ECP when appropriate) | `src/QscDspDevices/Protocol/QrcFramer.cs`, `JsonRpcDispatcher.cs`, JsonRpc/* (QRC); ECP backend planned in M-ECP | `tests/QscDspDevices.UnitTests/Protocol/*` (40+ tests for QRC) | ⚠ QRC complete in M2; ECP queued |
| 3.2 | Implement `BaseDevice` (Manufacturer=`QSC`, Model=`Q-SYS Core`) | `src/QscDspDevices/Plugin/QscDspTcp.cs:79-80` (constructor) | `QscDspTcpTests.Constructor_sets_Manufacturer_to_QSC_and_Model_to_QSysCore` | ✅ |
| 3.3 | Override `Connect()`/`Disconnect()` to manage TCP/IP | `src/QscDspDevices/Plugin/QscDspTcp.cs:170-187`, `src/QscDspDevices/Connectivity/ConnectionManager.cs` | `QscDspTcpTests.Connect_drives_*`, `ConnectionManagerTests.*`, integration `FakeServerEndToEndTests` | ✅ |
| 3.4 | Set `IsOnline` then call `NotifyOnlineStatus()` | `src/QscDspDevices/Plugin/QscDspTcp.cs:OnStateChanged` | `QscDspTcpTests.Connect_drives_IsOnline_true_then_NotifyOnlineStatus` (asserts ordering by reading IsOnline INSIDE the handler) | ✅ |
| 3.5 | Implement `IDsp`, `IAudioRoutable`, `IAudioZoneEnabler`, `IRedundancySupport`, `IDspLogicTriggerSupport` | `src/QscDspDevices/Plugin/QscDspTcp.cs` declares all; method bodies stub-and-log until M3-M6 fill them | `QscDspTcpTests.M3_audio_control_methods_log_Notice_and_return_documented_fallback_without_throwing` and `IRedundancySupport_properties_default_to_no_backup` | ⚠ Surface complete in M2; bodies fill in M3-M6 |

## 4. Restrictions (README §4)

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 4.1 | Internal thread count ≤ 3 | `src/QscDspDevices/Plugin/Threading/ThreadCensus.cs` runtime guard (FailFast in DEBUG, log + refuse in RELEASE) | `ThreadCensusTests.cs` (6 tests, including 3-concurrent-OK + 4th-rejected) | ✅ |
| 4.2 | No public async/await | architectural — every public method on `QscDspTcp` returns synchronously; internal `Task` use is private | qsc-critic check R5 + `QscDspTcpTests` (none of the public-surface methods return `Task`) | ✅ |
| 4.3 | Use `gcu_common_utils.NetComs.BasicTcpClient` | `src/QscDspDevices/Transport/BasicTcpClientTransport.cs` (production) wrapping the framework's `BasicTcpClient` | Compile-time reference; integration tests use `RawTcpTransport` only to avoid the framework stub's NotImplementedException | ✅ |
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
| 7.1 | Maintain connection until external `Disconnect()` | `src/QscDspDevices/Connectivity/ConnectionManager.cs` | `ConnectionManagerTests.Connect_drives_state_through_Connecting_to_Connected`, `FakeServerEndToEndTests.Connecting_to_a_FakeQrcServer_drives_state_into_Connected` | ✅ |
| 7.2 | Lost connection → reconnect immediately if no backup, else switch to backup | `Connectivity/ConnectionManager.cs` covers no-backup retry; backup path planned in M6 | `ConnectionManagerTests.Mid_flight_drop_triggers_reconnect_loop` (no-backup path) | ⚠ no-backup path complete; backup-failover in M6 |
| 7.3 | On failed connection: log error, wait 15s, retry until external Disconnect() | `src/QscDspDevices/Connectivity/ReconnectStrategy.cs` (constant 15s, deterministic-clock-tested) | `ReconnectStrategyTests.Interval_is_exactly_fifteen_seconds`, `ConnectionManagerTests.Failed_first_attempt_triggers_reconnect_after_exactly_fifteen_seconds` | ✅ |
| 7.4 | On any disconnect: update `IsOnline`, then call `NotifyOnlineStatus()` | `src/QscDspDevices/Plugin/QscDspTcp.cs:OnStateChanged`; manager fault-path transitions to Disconnected immediately (NOT 15s later) per integration test discovery | `QscDspTcpTests.Disconnect_drives_IsOnline_false_then_NotifyOnlineStatus` (asserts ordering by reading IsOnline INSIDE the handler) | ✅ |
| 7.5 | On successful connection: hydrate state of channels/routing/logic/snapshots | `src/QscDspDevices/Connectivity/IPostConnectAction.cs` (hook, default `NoopPostConnectAction`); concrete hydration lands in M3 | `ConnectionManagerTests` exercise the hook path; behavioural tests in M3 | ⏳ M3 (hook + default no-op shipped in M2) |

## 8. Sending/receiving (README §"Sending/Receiving")

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 8.1 | Commands sent ASAP | `src/QscDspDevices/Protocol/CommandQueue.cs` (Channel-backed FIFO; send-loop dequeues immediately) | `CommandQueueTests.Sequential_FIFO_preserves_id_order`, `Sequential_FIFO_preserves_id_order` (FsCheck property) | ✅ |
| 8.2 | Queue while sending; send next at next opportunity | `src/QscDspDevices/Protocol/CommandQueue.cs` (bounded 1024 with oldest-drop saturation policy) | `CommandQueueTests.Saturation_drops_oldest_and_increments_drop_counter` | ✅ |
| 8.3 | FIFO order | `src/QscDspDevices/Protocol/CommandQueue.cs` (`Channel.CreateBounded<T>` + single-reader) | `CommandQueueProperties.Sequential_FIFO_preserves_id_order` (FsCheck, random N up to 1024) | ✅ |
| 8.4 | Refuse send/queue while disconnected; log error | `src/QscDspDevices/Protocol/CommandQueue.cs:TryEnqueue` (state-locked check, logs Error, returns false) | `CommandQueueTests.TryEnqueue_when_not_accepting_returns_false_and_logs_error` (with TestLoggerSink assertion); `FakeServerEndToEndTests.Refusing_send_while_disconnected_logs_an_error` | ✅ |
| 8.5 | Clear queue on any disconnect | `src/QscDspDevices/Protocol/CommandQueue.cs:Drain` (called by `ConnectionManager.CleanupAfterDisconnect`); logs Notice with discard count | `CommandQueueTests.Drain_discards_pending_entries`, `ConnectionManagerTests.Disconnect_drains_queue_and_reaches_Disconnected` | ✅ |
| 8.6 | Maintain up-to-date control state via polling/subscriptions | `src/QscDspDevices/Protocol/JsonRpcDispatcher.cs:IAutoPollSubscription` (registration surface); concrete subscribers land in M3 | `JsonRpcDispatcherTests.AutoPoll_push_routes_to_subscription_not_to_pending_request` | ⚠ Surface complete in M2; consumer in M3 |
| 8.7 | On state update: update internal state THEN notify subscribers | `src/QscDspDevices/Plugin/QscDspTcp.cs:OnStateChanged` enforces this for IsOnline; channel/preset/router state updates land in M3+ | `QscDspTcpTests.Connect_drives_IsOnline_true_then_NotifyOnlineStatus` (asserts state-before-notify by reading IsOnline INSIDE the handler) | ⚠ IsOnline pattern shipped in M2; channel-state pattern in M3 |

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
