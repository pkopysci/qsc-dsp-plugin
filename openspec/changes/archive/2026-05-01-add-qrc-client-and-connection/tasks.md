# Tasks — add-qrc-client-and-connection

## 1. QscDspTcp public class skeleton

- [x] 1.1 Create `src/QscDspDevices/Plugin/QscDspTcp.cs` inheriting `BaseDevice` and implementing `IDsp`, `IAudioRoutable`, `IAudioZoneEnabler`, `IDspLogicTriggerSupport`, `IRedundancySupport`, `IPowerControllable`, `ITcpDevice`. (Many methods are stubs returning the documented fallback until later milestones.)
- [x] 1.2 Constructor sets `Manufacturer = "QSC"` and `Model = "Q-SYS Core"` per README §3.
- [x] 1.3 Implement `Initialize(string hostId, int coreId, string hostname, int port, string username, string password)` to capture configuration into private fields and call `IsInitialized = true`. Does not connect.
- [x] 1.4 Override `Connect()` to start the connection manager (no-op if already connecting/connected).
- [x] 1.5 Override `Disconnect()` to drive the manager into Disconnecting → Disconnected. Joins all session threads.
- [x] 1.6 Implement `IDisposable` per the standard pattern. Idempotent.

## 2. Transport layer

- [x] 2.1 Define `Transport/IConnectionTransport.cs` (Connect/Disconnect/Send byte[]/RxReceived event/ConnectionFailed event/IsConnected property).
- [x] 2.2 Implement `Transport/BasicTcpClientTransport.cs` wrapping `gcu_common_utils.NetComs.BasicTcpClient`. README §4 makes this the only sanctioned client for production.
- [x] 2.3 Implement `Transport/RawTcpTransport.cs` (TestSupport only) — a thin wrapper around `System.Net.Sockets.TcpClient` for integration tests against the FakeQrcServer (avoids touching the BasicTcpClient stub).
- [x] 2.4 Define `Transport/IFaultInjector.cs` — test-only interface for failure-injection knobs. Production code holds a no-op implementation. **Delivered as a different shape:** the failure-injection knobs live as five toggle methods on `FakeQrcServer` itself (`DropConnection()`, `DelayResponseMs(int)`, `EmitMalformed()`, `RespondWithStandbyError()`, `RequireLogonPin()`). Production code has no fault-injector seam — there is nothing for it to attach to that the transport stub does not already provide.

## 3. Protocol layer — framing

- [x] 3.1 Implement `Protocol/QrcFramer.cs` — null-byte (`\x00`) frame splitter and builder. UTF-8.
- [x] 3.2 Bounded read buffer: 16 KiB initial, doubling up to a configurable max (default 16 MiB). Oversize frames raise `FrameTooLargeException` and force connection drop.
- [x] 3.3 Outbound: `Encode(JsonRpcRequest)` → `byte[]` ending in `\x00`.

## 4. Protocol layer — JSON-RPC models and dispatcher

- [x] 4.1 `Protocol/JsonRpc/JsonRpcRequest.cs`, `JsonRpcResponse.cs`, `JsonRpcError.cs`, `JsonRpcNotification.cs` — Newtonsoft.Json-serializable POCOs.
- [x] 4.2 `Protocol/QrcErrorCode.cs` — enum mapping the standard JSON-RPC codes plus QSC-specific codes documented in `research/QRC_PROTOCOL.md` §10.2 (`-32604` Standby, `5` ChangeGroupsExhausted, `6` UnknownChangeGroup, `7` UnknownComponentName, `8` UnknownControl, `9` IllegalMixerChannelIndex, `10` LogonRequired).
- [x] 4.3 `Protocol/JsonRpcDispatcher.cs` — id-based response correlation, AutoPoll subscription map, server-notification routing.
- [x] 4.4 Monotonic int64 id generator (`Protocol/IdGenerator.cs`).

## 5. Protocol layer — command queue & keepalive

- [x] 5.1 `Protocol/CommandQueue.cs` backed by `Channel<QrcRequest>`. Bound 1024. `TryEnqueue` returns false + logs when not accepting. `Drain()` clears + flips not-accepting.
- [x] 5.2 `Protocol/KeepaliveTimer.cs` — emits `NoOp` JSON-RPC every 30s of outbound silence. Resets on every outbound frame.
- [ ] 5.3 Logon-payload redaction in framer's debug-log path (so `Logger.Debug` never prints credentials). **Deferred to M3.** No `Logon` JSON-RPC call is emitted in M2 — `Initialize` captures `username`/`password` but discards them (`src/QscDspDevices/Plugin/QscDspTcp.cs:158-162`). Redaction lands when M3 wires the actual Logon post-connect action.

## 6. Connectivity layer

- [x] 6.1 `Connectivity/ConnectionManager.cs` — state machine `Disconnected → Connecting → Connected → Disconnecting → Disconnected`.
- [x] 6.2 Sets `BaseDevice.IsOnline` THEN calls `BaseDevice.NotifyOnlineStatus()` per README §3 ordering.
- [x] 6.3 `Connectivity/ReconnectStrategy.cs` — fixed 15-second wait between attempts; loops until `Disconnect()` or success. README §"Device Connection" literal.
- [x] 6.4 Hydration hook (`IPostConnectAction`) — empty default impl; M3 fills it.
- [x] 6.5 On every transition into Disconnected: `CommandQueue.Drain()` + join all session threads + log Notice.

## 7. Threading budget

- [x] 7.1 `Plugin/Threading/ThreadCensus.cs` — registers running plugin-owned threads. Fails loudly if more than 3 alive.
- [x] 7.2 `Plugin/Threading/IQrcClock.cs`, `Plugin/Threading/SystemClock.cs` — production wall-clock implementation.
- [ ] 7.3 `Plugin/Threading/PluginTimer.cs` — single `Task`-driven timer thread shared by KeepaliveTimer + ReconnectStrategy. **Deferred to M3.** M2 ships `KeepaliveTimer` and `ReconnectStrategy` as standalone components driven by the single `RunSessionAsync` task on the threadpool, not a dedicated timer thread. The dedicated send/receive/timer trio lands when M3 wires the active change-group subscriptions and we know the steady-state work pattern.

## 8. TestSupport

- [x] 8.1 `tests/QscDspDevices.TestSupport/FakeQrcServer.cs` — TCP listener implementing the QRC protocol per `research/QRC_PROTOCOL.md`.
- [x] 8.2 Failure-injection knobs: `DropConnection()`, `DelayResponseMs(int)`, `EmitMalformed()`, `RespondWithStandbyError()`, `WithdrawKeepaliveTolerance()`.
- [x] 8.3 `tests/QscDspDevices.TestSupport/DeterministicClock.cs` — `IQrcClock` impl with manual time advance.
- [x] 8.4 `tests/QscDspDevices.TestSupport/TestLoggerSink.cs` — captures `Logger.Error/Warn/Notice/Debug` calls in-memory for assertions.

## 9. Unit tests

- [x] 9.1 `QrcFramerTests` — round-trip, fragmentation across reads, multiple frames per buffer, oversize rejection.
- [x] 9.2 `JsonRpcDispatcherTests` — id correlation happy path, AutoPoll-id reuse routes to subscription, notifications fire event, unknown-id logs warn.
- [x] 9.3 `CommandQueueTests` — refuses while not accepting, FIFO under sequential, drain-and-flip on disconnect, saturation drops oldest.
- [x] 9.4 `KeepaliveTimerTests` — emits NoOp after 30s silence, resets on outbound write (deterministic clock).
- [x] 9.5 `ConnectionManagerTests` — every state transition, IsOnline-before-NotifyOnlineStatus invariant, queue cleared on disconnect.
- [x] 9.6 `ReconnectStrategyTests` — exact 15s interval (deterministic clock), loops until Disconnect, no thread leak.
- [x] 9.7 `ThreadCensusTests` — registers + unregisters cleanly, breach trips guard.
- [x] 9.8 `BasicTcpClientTransportTests` — 12 cases covering constructor validation, dispose pattern, `Connected` accessor and `Send` guards. Pass-2 critic flagged that two of the cases are tautologies under the framework stub; comment block at the top of the test class explains what is and isn't covered (`tests/QscDspDevices.UnitTests/Transport/BasicTcpClientTransportTests.cs`).

## 10. Property tests (FsCheck)

- [x] 10.1 `QrcFramerProperties` — random byte streams produce no exceptions other than documented `FrameTooLargeException` and JSON-RPC parse errors. Round-trip property: encode then decode any valid request returns the original.
- [x] 10.2 `CommandQueueProperties` — under random concurrent producers, dequeued order matches enqueue order across single producer; monotonic timestamps preserved.

## 11. Integration tests (against FakeQrcServer)

- [x] 11.1 `Connect_succeeds_and_drives_IsOnline_then_NotifyOnlineStatus`.
- [ ] 11.2 `Connect_failure_logs_error_and_retries_after_15s` (deterministic clock). **Covered at unit level in M2 / integration form deferred to M3.** `tests/QscDspDevices.UnitTests/Connectivity/ConnectionManagerTests.cs` exercises the 15s reconnect cadence against the deterministic clock; the integration-level variant requires a `FakeQrcServer` that can refuse the first N attempts and the deterministic clock wired into the production session loop, which conflicts with M2's "real Task.Delay" choice.
- [x] 11.3 `Mid_flight_socket_drop_triggers_15s_reconnect_loop_until_success`.
- [x] 11.4 `Disconnect_drains_command_queue_and_returns_to_Disconnected` (`tests/QscDspDevices.IntegrationTests/Connection/FakeServerEndToEndTests.cs:85`). The "joins_session_threads" half of the original spec is N/A in M2: the session is one threadpool task, not a dedicated thread; `ThreadCensus` returning to 0 after disconnect is verified at unit level (`tests/QscDspDevices.UnitTests/Plugin/QscDspTcpTests.cs:354`). M3 will revisit this when the dedicated send/receive/timer threads exist.
- [ ] 11.5 `Outbound_silence_30s_emits_NoOp`. **Covered at unit level only.** `KeepaliveTimerTests` pins the 30s deterministic-clock behaviour; the integration form requires the production session loop to use the deterministic clock for keepalive, which lands in M3.
- [ ] 11.6 `Standby_error_-32604_logs_warn_and_does_not_retry_command`. **Covered at unit level.** `QrcErrorClassifierTests` pins the classification (Standby is non-fatal-but-should-not-retry); the full retry-suppression behaviour is M6 (failover) territory.
- [ ] 11.7 `Malformed_server_frame_logs_error_and_drops_connection`. **Covered at unit level.** `QrcFramerTests` covers framer-level malformed handling and `FakeServerEndToEndTests` covers the connection-drop path; the combined integration test (server emits malformed JSON → framer rejects → manager drops connection → log captured) is straight glue and is left to M3 along with the `EmitMalformed()` knob's first real use.
- [x] 11.8 `Refuse_send_while_disconnected_logs_error_and_returns_silently`.

## 12. Documentation

- [x] 12.1 Update `ARCHITECTURE.md` with the actual state-machine diagram, lock-order specifics, ThreadCensus details (replace the M1 placeholders with real text + file:line references).
- [x] 12.2 Update `SPEC_COMPLIANCE.md` rows the milestone discharges (3.2-3.4, 4.1, 4.3, 7.1-7.4, 8.1-8.5) from ⏳ to ✅ with file:line citations.

## 13. Build, format, and review gates

- [x] 13.1 `dotnet build QscDspDevices.sln`: 0 warnings, 0 errors (Debug + Release).
- [x] 13.2 `dotnet format QscDspDevices.sln --verify-no-changes`: clean.
- [x] 13.3 `dotnet test QscDspDevices.sln`: all tests pass.
- [x] 13.4 Coverage on `QscDspDevices.dll`: 90.0% line / 84.5% branch / 94% method (CI gate).
- [x] 13.5 DLL size (`-c Release`): ≤ 500 KB.
- [x] 13.6 `openspec validate add-qrc-client-and-connection --strict`: passes.
- [x] 13.7 Run qsc-critic agent locally; save report to `openspec/changes/add-qrc-client-and-connection/REVIEW.md`. Three passes recorded; pass-3 verdict ⚠️ ship with caveats. All blockers addressed; remaining concerns documented and deferred to M3 with rationale.

## 14. Commit + PR

- [x] 14.1 Commit incrementally — one logical commit per major component (framer, dispatcher, queue, etc.) so the merge log reads as a build-up.
- [x] 14.2 Opened PR #3 (https://github.com/pkopysci/qsc-dsp-plugin/pull/3) with the spec-id reference and compliance citations; CI green; user-approved merge to main.
