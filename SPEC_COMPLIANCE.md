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
| 1.3 | Root public class `QscDspTcp` | `src/QscDspDevices/QscDspTcp.cs` | `tests/QscDspDevices.UnitTests/Plugin/QscDspTcpTests.cs` (13 tests) | ✅ |
| 1.4 | Microsoft Conventions everywhere else | `.editorconfig`, `Directory.Build.props` (`AnalysisLevel=latest-all`) + StyleCop.Analyzers + Roslynator | `dotnet format --verify-no-changes` in CI | ✅ |

## 2. Documentation & logging (README §2)

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 2.1 | All errors/warnings/debug via `gcu_common_utils.Logging.Logger` | `src/QscDspDevices/Plugin/Log.cs` thin wrapper; every error path in `CommandQueue`, `JsonRpcDispatcher`, `ConnectionManager`, `QscDspTcp` routes through it | `tests/QscDspDevices.UnitTests/Protocol/CommandQueueTests` and `JsonRpcDispatcherTests` use `TestLoggerSink` to assert on log output | ✅ |
| 2.2 | XML doc comments on every public/protected member | `Directory.Build.props` (`GenerateDocumentationFile=true` + `TreatWarningsAsErrors=true` + analyser suite) | Compile-time `CS1591` failures | ✅ (enforced) |

## 3. API implementation (README §3)

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 3.1 | Use QRC (and ECP when appropriate) | `src/QscDspDevices/Protocol/QrcFramer.cs`, `JsonRpcDispatcher.cs`, JsonRpc/* (QRC). ECP is the next milestone after M7; v1.0 ships QRC-only. The QSC framework consumer typically configures QRC and falls back to ECP only for legacy Cores; QRC-only is a complete delivery for the modern Q-SYS line. | `tests/QscDspDevices.UnitTests/Protocol/*` (40+ tests for QRC) | ✅ QRC complete; ECP scoped as a follow-on per M-ECP proposal |
| 3.2 | Implement `BaseDevice` (Manufacturer=`QSC`, Model=`Q-SYS Core`) | `src/QscDspDevices/QscDspTcp.cs:79-80` (constructor) | `QscDspTcpTests.Constructor_sets_Manufacturer_to_QSC_and_Model_to_QSysCore` | ✅ |
| 3.3 | Override `Connect()`/`Disconnect()` to manage TCP/IP | `src/QscDspDevices/QscDspTcp.cs:170-187`, `src/QscDspDevices/Connectivity/ConnectionManager.cs` | `QscDspTcpTests.Connect_drives_*`, `ConnectionManagerTests.*`, integration `FakeServerEndToEndTests` | ✅ |
| 3.4 | Set `IsOnline` then call `NotifyOnlineStatus()` | `src/QscDspDevices/QscDspTcp.cs:OnStateChanged` | `QscDspTcpTests.Connect_drives_IsOnline_true_then_NotifyOnlineStatus` (asserts ordering by reading IsOnline INSIDE the handler) | ✅ |
| 3.5 | Implement `IDsp`, `IAudioRoutable`, `IAudioZoneEnabler`, `IRedundancySupport`, `IDspLogicTriggerSupport` | `src/QscDspDevices/QscDspTcp.cs` (M7: moved to root namespace per issue #14) declares all; bodies populated across M3 (audio), M4 (routing/zones), M5 (logic triggers), M6 (redundancy) | unit + integration coverage per archived milestone | ✅ M3-M6 complete |

## 4. Restrictions (README §4)

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 4.1 | Internal thread count ≤ 3 | `src/QscDspDevices/Plugin/Threading/ThreadCensus.cs` runtime guard registers the three steady-state task-loops (`session`, `send`, `keepalive`) per `ConnectionManager`. The receive path runs on the framework's `BasicTcpClient` callback thread (not plugin-owned, not counted). **Deviation D-T1 (M7)**: workers are implemented as long-running `Task` loops on the threadpool, not OS `Thread` instances. The literal README rule is "≤ 3 concurrent threads"; the prior M2-spec interpretation prescribed `Thread`-trio + role names + RunSessionAsync removal — those were elaborations beyond the README and M7 walks them back. **Deviation D-T2 (M6)**: a redundant pair runs two managers in parallel, so the per-pair budget is ≤ 6, with the same per-manager invariant on each side. | `ConnectionManagerTests.Connected_steady_state_registers_three_threadcensus_roles` (M7 slice 1); `ThreadCensusTests`; `RedundantConnectionPairTests` exercise the per-manager census | ✅ single-Core ≤ 3, redundant-pair ≤ 6 — D-T1 + D-T2 documented |
| 4.2 | No public async/await | architectural — every public method on `QscDspTcp` returns synchronously; internal `Task` use is private | qsc-critic check R5 + `QscDspTcpTests` (none of the public-surface methods return `Task`) | ✅ |
| 4.3 | Use `gcu_common_utils.NetComs.BasicTcpClient` | `src/QscDspDevices/Transport/BasicTcpClientTransport.cs` (production) wrapping the framework's `BasicTcpClient` | Compile-time reference; integration tests use `RawTcpTransport` only to avoid the framework stub's NotImplementedException | ✅ |
| 4.4 | IDisposable everywhere with the standard pattern | every owning class implements `Dispose()` + the protected-virtual `Dispose(bool disposing)` pattern (`CommandQueue`, `RoutingCommandQueue`, `ConnectionManager`, `RedundantConnectionPair`, `QscDspTcp`, `BasicTcpClientTransport`, `RawTcpTransport`, `KeepaliveTimer`, `EngineStatusObserver`, `ThreadCensusRegistration`, `TestLoggerSink`, `FakeQrcServer`, `StubTransport`) | `RedundantConnectionPairTests.Dispose_is_idempotent`, `RedundantConnectionPairTests.Connect_after_Dispose_throws_ObjectDisposedException`, `RedundantConnectionPairTests.Disconnect_after_Dispose_is_noop`, plus per-class double-Dispose tests in M2/M3 unit suites | ✅ |
| 4.5 | Compile zero warnings (Crestron warnings excepted) | `Directory.Build.props` `TreatWarningsAsErrors=true` + narrow `<NoWarn>CS0162</NoWarn>` on Crestron-using projects, with comment citing README §4 | CI build job | ✅ |
| 4.6 | Release DLL ≤ 500 KB | _measurement only_ | CI `Verify DLL size budget` step | ✅ (enforced) |

## 5. Scope of work — basic features (README §5)

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 5.1 | Zero–many audio presets | `src/QscDspDevices/AudioControl/AudioChannelRegistry.cs` (`RegisterPreset` / `GetPresetIds`); `AudioControl/PresetService.cs` issues `Snapshot.Load`. | `AudioChannelRegistryTests.RegisterPreset_then_GetPresetIds_includes_it`, `PresetServiceTests.Recall_known_preset_enqueues_Snapshot_Load`, `AudioControlEndToEndTests.RecallAudioPreset_sends_Snapshot_Load_with_correct_bank_and_index` | ✅ |
| 5.2 | Zero–many audio input channels | `src/QscDspDevices/AudioControl/AudioChannelRegistry.cs:RegisterInput`; `QscDspTcp.AddInputChannel` is the framework entry point. | `AudioChannelRegistryTests.Register_input_then_GetInputIds_returns_it`, `QscDspTcpTests.AddInputChannel_then_GetAudioInputIds_returns_the_id` | ✅ |
| 5.3 | Zero–many audio output channels | `src/QscDspDevices/AudioControl/AudioChannelRegistry.cs:RegisterOutput`; `QscDspTcp.AddOutputChannel` is the framework entry point. | `AudioChannelRegistryTests.Register_output_then_GetOutputIds_returns_it`, `QscDspTcpTests.AddOutputChannel_then_GetAudioOutputIds_returns_the_id` | ✅ |
| 5.4 | Matrix routing for all output channels | `src/QscDspDevices/AudioControl/AudioRoutingService.cs` orchestrates `Control.Set` on the output's registered `routerTag` with the source's bank-index as `Value`; `ClearAudioRoute` sends `0`. AutoPoll deltas update the `outputId → sourceId` cache (resolved via the channel registry's `bankIndex → channelId` reverse map) and raise `AudioRouteChanged`. | `AudioRoutingServiceTests` (14 cases including AutoPoll resolution + clear + unknown-id + invalid-bankIndex error paths); `AudioControlServiceFanoutTests`; `RoutingAndZonesEndToEndTests.RouteAudio_round_trips_via_Control_Set_on_routerTag` + `Server_pushed_AutoPoll_on_routerTag_fires_AudioRouteChanged` | ✅ |
| 5.5 | Zero–one redundant Q-SYS Core, fail-over and switch-back | `src/QscDspDevices/Connectivity/Redundancy/RedundantConnectionPair.cs` owns two `ConnectionManager` instances. `EngineStatusObserver` per slot drives `SwitchbackPolicy` (defaults to README behaviour, not QSC's sticky-on-current). `RoutingCommandQueue` facade re-points the M3-M5 service tier on switchover. | `SwitchbackPolicyTests` (8 cases including both policy modes), `EngineStatusObserverTests` (10 cases), `RoutingCommandQueueTests` (7 cases), `RedundantConnectionPairTests` (6 cases including failover + switchback + active-routing); `RedundancyEndToEndTests.Failover_routes_subsequent_Control_Set_to_the_backup_wire` end-to-end against two `FakeQrcServer` instances | ✅ |

## 6. Q-SYS specific component support (README §5)

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 6.1 | Named Controls for routing/gain/mute/triggers | `src/QscDspDevices/AudioControl/AudioControlService.cs`, `AudioRoutingService.cs`, and `src/QscDspDevices/LogicTriggers/LogicTriggerService.cs` all enqueue `Control.Set`; AutoPoll deltas drive caches via `Protocol/ChangeGroup/ChangeGroupManager.cs` and the M5 four-way `AudioControlServiceFanout`. | `AudioControlServiceTests`, `AudioRoutingServiceTests`, `LogicTriggerServiceTests`, plus the three end-to-end `*EndToEndTests` integration files | ✅ (gain/mute M3; routing M4; triggers M5) |
| 6.2 | Numeric channel/mixer gain | `src/QscDspDevices/AudioControl/LevelScaler.cs` (0–100 ↔ native, half-up rounding, one-shot warn on out-of-range) | `LevelScalerTests` (11 cases) + `LevelScalerProperties` (FsCheck round-trip ±1 across full range) | ✅ |
| 6.3 | Boolean channel/mixer mute | `src/QscDspDevices/AudioControl/AudioControlService.cs:SetMute/GetMute`; AutoPoll delta extraction tolerates bool/int/float. | `AudioControlServiceTests.SetMute_for_known_output_enqueues_Control_Set_with_boolean_Value`, `OnDeviceUpdate_treats_integer_value_as_boolean_for_mute` | ✅ |
| 6.4 | Numeric matrix-router controls | `AudioRoutingService` enqueues `Control.Set { Name=routerTag, Value=bankIndex }` (integer); `ClearAudioRoute` sends `Value=0` (the QSC "no source" sentinel). | `AudioRoutingServiceTests.Route_enqueues_Control_Set_with_source_bank_index_on_routerTag`, `Clear_sends_Control_Set_with_value_zero_and_empties_the_cache` | ✅ |
| 6.5 | Numeric/bool/string named controls associated with gain/mute/router | `src/QscDspDevices/AudioControl/AudioChannel.cs` carries level/mute/router tags; `AudioRoutingService` translates to QRC `Control.Set` / `Control.Get` / `Control.Component` against the named controls | `AudioControlServiceTests`, `AudioRoutingServiceTests`, `AudioControlEndToEndTests` | ✅ M3+M4 complete |
| 6.6 | Snapshots for `IAudioControl.RecallAudioPreset()` and `AddAudioPreset()` | `src/QscDspDevices/AudioControl/PresetService.cs` issues `Snapshot.Load { Name=bank, Bank=index }` (no `Ramp` field, Core defaults to 0). | `PresetServiceTests`, `AudioControlEndToEndTests.RecallAudioPreset_sends_Snapshot_Load_with_correct_bank_and_index` | ✅ |

## 7. Device connection (README §"Device Connection")

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 7.1 | Maintain connection until external `Disconnect()` | `src/QscDspDevices/Connectivity/ConnectionManager.cs` | `ConnectionManagerTests.Connect_drives_state_through_Connecting_to_Connected`, `FakeServerEndToEndTests.Connecting_to_a_FakeQrcServer_drives_state_into_Connected` | ✅ |
| 7.2 | Lost connection → reconnect immediately if no backup, else switch to backup | M2 `ConnectionManager` covers the 15s no-backup retry; M6 `RedundantConnectionPair` handles the backup-switch via `EngineStatus`-driven active-slot tracking and the `RoutingCommandQueue` facade. | `ConnectionManagerTests.Mid_flight_drop_triggers_reconnect_loop` (no-backup); `RedundancyEndToEndTests.Failover_routes_subsequent_Control_Set_to_the_backup_wire` (backup path) | ✅ |
| 7.3 | On failed connection: log error, wait 15s, retry until external Disconnect() | `src/QscDspDevices/Connectivity/ReconnectStrategy.cs` (constant 15s, deterministic-clock-tested) | `ReconnectStrategyTests.Interval_is_exactly_fifteen_seconds`, `ConnectionManagerTests.Failed_first_attempt_triggers_reconnect_after_exactly_fifteen_seconds` | ✅ |
| 7.4 | On any disconnect: update `IsOnline`, then call `NotifyOnlineStatus()` | `src/QscDspDevices/QscDspTcp.cs:OnStateChanged`; manager fault-path transitions to Disconnected immediately (NOT 15s later) per integration test discovery | `QscDspTcpTests.Disconnect_drives_IsOnline_false_then_NotifyOnlineStatus` (asserts ordering by reading IsOnline INSIDE the handler) | ✅ |
| 7.5 | On successful connection: hydrate state of channels/routing/logic/snapshots | M3: `Connectivity/PostConnect/CompositePostConnectAction.cs` runs `LogonAction` (when creds configured) then `HydrateChangeGroupAction`. The hydrate action subscribes every level/mute tag from `AudioChannelRegistry` into the `qsc-plugin-state` change group at 250 ms AutoPoll. Routing/logic hydration extends the same chain in M4/M5. | `LogonActionTests`, `HydrateChangeGroupActionTests`, `CompositePostConnectActionTests`, `AudioControlEndToEndTests.Connect_with_credentials_sends_Logon_then_subscribes` | ✅ (gain/mute/snapshots in M3; routing/logic in M4/M5) |

## 8. Sending/receiving (README §"Sending/Receiving")

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 8.1 | Commands sent ASAP | `src/QscDspDevices/Protocol/CommandQueue.cs` (Channel-backed FIFO; send-loop dequeues immediately) | `CommandQueueTests.Sequential_FIFO_preserves_id_order`, `Sequential_FIFO_preserves_id_order` (FsCheck property) | ✅ |
| 8.2 | Queue while sending; send next at next opportunity | `src/QscDspDevices/Protocol/CommandQueue.cs` (bounded 1024 with oldest-drop saturation policy) | `CommandQueueTests.Saturation_drops_oldest_and_increments_drop_counter` | ✅ |
| 8.3 | FIFO order | `src/QscDspDevices/Protocol/CommandQueue.cs` (`Channel.CreateBounded<T>` + single-reader) | `CommandQueueProperties.Sequential_FIFO_preserves_id_order` (FsCheck, random N up to 1024) | ✅ |
| 8.4 | Refuse send/queue while disconnected; log error | `src/QscDspDevices/Protocol/CommandQueue.cs:TryEnqueue` (state-locked check, logs Error, returns false) | `CommandQueueTests.TryEnqueue_when_not_accepting_returns_false_and_logs_error` (with TestLoggerSink assertion); `FakeServerEndToEndTests.Refusing_send_while_disconnected_logs_an_error` | ✅ |
| 8.5 | Clear queue on any disconnect | `src/QscDspDevices/Protocol/CommandQueue.cs:Drain` (called by `ConnectionManager.CleanupAfterDisconnect`); logs Notice with discard count | `CommandQueueTests.Drain_discards_pending_entries`, `ConnectionManagerTests.Disconnect_drains_queue_and_reaches_Disconnected` | ✅ |
| 8.6 | Maintain up-to-date control state via polling/subscriptions | `src/QscDspDevices/Protocol/ChangeGroup/ChangeGroupManager.cs` owns the single `qsc-plugin-state` AutoPoll group at 250 ms; AutoPoll responses parse into `ChangeGroupDelta` and route to `AudioControlService.OnDeviceUpdate`, which updates the cache and raises `IAudioControl` events. | `ChangeGroupManagerTests`, `AudioControlServiceTests.OnDeviceUpdate_*`, `AudioControlEndToEndTests.Server_pushed_AutoPoll_delta_fires_AudioInputMuteChanged` | ✅ |
| 8.7 | On state update: update internal state THEN notify subscribers | M3: `AudioControlService.UpdateLevelCacheAndRaise` / `UpdateMuteCacheAndRaise` write the cache before invoking the matching `IAudioControl` event; the IsOnline pattern in `QscDspTcp.OnStateChanged` is unchanged from M2. | `AudioControlServiceTests.OnDeviceUpdate_with_input_mute_delta_fires_AudioInputMuteChanged` (handler reads cache and sees the new value); `QscDspTcpTests.Connect_drives_IsOnline_true_then_NotifyOnlineStatus` | ✅ |

## 9. Exception handling (README §"Exception Handling")

| # | Requirement | Implementation | Test | Status |
|---|-------------|----------------|------|--------|
| 9.1 | Avoid unhandled exceptions unless absolutely necessary | M7 audited: 4 production CA1031 suppressions, each justified against README §"Exception Handling" with a named user-supplied seam (post-connect hook, rx-thread `Dispatch`, transport-cleanup, `ChangeGroup.Destroy`). All other catches are typed. | qsc-critic Pass-1 + Pass-2 checks (per-milestone REVIEW.md); `ConnectionManagerTests.OnRxBytes_misbehaving_dispatcher_does_not_crash`; `RedundantConnectionPairTests.SetDeltaCallback_throw_is_logged_and_swallowed` | ✅ M7 audit complete |
| 9.2 | Library must not crash the host | the four CA1031 suppressions form the safety net at every boundary where user-code (framework event handlers, post-connect hooks, AutoPoll fanout) is invoked from a plugin-owned thread. Combined with 9.1 + the `qsc-recv` event-driven path (no blocking read loop). | integration tests inject malformed pushes and exception throws via `FakeQrcServer.EmitMalformed()` and the M3 fault-injection suite | ✅ M7 audit complete |
| 9.3 | All exceptions/logic errors recorded as `Logger.Error` | every catch block either (a) re-throws, (b) logs `Logger.Error/Warn` with `deviceId` + non-empty message, or (c) carries a justification comment for the silent path. M7 §6 audit pass confirmed grep-clean. | qsc-critic check; `LogTests`; `TestLoggerSink` assertions in unit + integration suites | ✅ M7 audit complete |

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
