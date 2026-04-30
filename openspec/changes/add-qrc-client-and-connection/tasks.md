# Tasks — add-qrc-client-and-connection

## 1. QscDspTcp public class skeleton

- [ ] 1.1 Create `src/QscDspDevices/Plugin/QscDspTcp.cs` inheriting `BaseDevice` and implementing `IDsp`, `IAudioRoutable`, `IAudioZoneEnabler`, `IDspLogicTriggerSupport`, `IRedundancySupport`, `IPowerControllable`, `ITcpDevice`. (Many methods are stubs returning the documented fallback until later milestones.)
- [ ] 1.2 Constructor sets `Manufacturer = "QSC"` and `Model = "Q-SYS Core"` per README §3.
- [ ] 1.3 Implement `Initialize(string hostId, int coreId, string hostname, int port, string username, string password)` to capture configuration into private fields and call `IsInitialized = true`. Does not connect.
- [ ] 1.4 Override `Connect()` to start the connection manager (no-op if already connecting/connected).
- [ ] 1.5 Override `Disconnect()` to drive the manager into Disconnecting → Disconnected. Joins all session threads.
- [ ] 1.6 Implement `IDisposable` per the standard pattern. Idempotent.

## 2. Transport layer

- [ ] 2.1 Define `Transport/IConnectionTransport.cs` (Connect/Disconnect/Send byte[]/RxReceived event/ConnectionFailed event/IsConnected property).
- [ ] 2.2 Implement `Transport/BasicTcpClientTransport.cs` wrapping `gcu_common_utils.NetComs.BasicTcpClient`. README §4 makes this the only sanctioned client for production.
- [ ] 2.3 Implement `Transport/RawTcpTransport.cs` (TestSupport only) — a thin wrapper around `System.Net.Sockets.TcpClient` for integration tests against the FakeQrcServer (avoids touching the BasicTcpClient stub).
- [ ] 2.4 Define `Transport/IFaultInjector.cs` — test-only interface for failure-injection knobs. Production code holds a no-op implementation.

## 3. Protocol layer — framing

- [ ] 3.1 Implement `Protocol/QrcFramer.cs` — null-byte (`\x00`) frame splitter and builder. UTF-8.
- [ ] 3.2 Bounded read buffer: 16 KiB initial, doubling up to a configurable max (default 16 MiB). Oversize frames raise `FrameTooLargeException` and force connection drop.
- [ ] 3.3 Outbound: `Encode(JsonRpcRequest)` → `byte[]` ending in `\x00`.

## 4. Protocol layer — JSON-RPC models and dispatcher

- [ ] 4.1 `Protocol/JsonRpc/JsonRpcRequest.cs`, `JsonRpcResponse.cs`, `JsonRpcError.cs`, `JsonRpcNotification.cs` — Newtonsoft.Json-serializable POCOs.
- [ ] 4.2 `Protocol/QrcErrorCode.cs` — enum mapping the standard JSON-RPC codes plus QSC-specific codes documented in `research/QRC_PROTOCOL.md` §10.2 (`-32604` Standby, `5` ChangeGroupsExhausted, `6` UnknownChangeGroup, `7` UnknownComponentName, `8` UnknownControl, `9` IllegalMixerChannelIndex, `10` LogonRequired).
- [ ] 4.3 `Protocol/JsonRpcDispatcher.cs` — id-based response correlation, AutoPoll subscription map, server-notification routing.
- [ ] 4.4 Monotonic int64 id generator (`Protocol/IdGenerator.cs`).

## 5. Protocol layer — command queue & keepalive

- [ ] 5.1 `Protocol/CommandQueue.cs` backed by `Channel<QrcRequest>`. Bound 1024. `TryEnqueue` returns false + logs when not accepting. `Drain()` clears + flips not-accepting.
- [ ] 5.2 `Protocol/KeepaliveTimer.cs` — emits `NoOp` JSON-RPC every 30s of outbound silence. Resets on every outbound frame.
- [ ] 5.3 Logon-payload redaction in framer's debug-log path (so `Logger.Debug` never prints credentials).

## 6. Connectivity layer

- [ ] 6.1 `Connectivity/ConnectionManager.cs` — state machine `Disconnected → Connecting → Connected → Disconnecting → Disconnected`.
- [ ] 6.2 Sets `BaseDevice.IsOnline` THEN calls `BaseDevice.NotifyOnlineStatus()` per README §3 ordering.
- [ ] 6.3 `Connectivity/ReconnectStrategy.cs` — fixed 15-second wait between attempts; loops until `Disconnect()` or success. README §"Device Connection" literal.
- [ ] 6.4 Hydration hook (`IPostConnectAction`) — empty default impl; M3 fills it.
- [ ] 6.5 On every transition into Disconnected: `CommandQueue.Drain()` + join all session threads + log Notice.

## 7. Threading budget

- [ ] 7.1 `Plugin/Threading/ThreadCensus.cs` — registers running plugin-owned threads. Fails loudly if more than 3 alive.
- [ ] 7.2 `Plugin/Threading/IQrcClock.cs`, `Plugin/Threading/SystemClock.cs` — production wall-clock implementation.
- [ ] 7.3 `Plugin/Threading/PluginTimer.cs` — single `Task`-driven timer thread shared by KeepaliveTimer + ReconnectStrategy.

## 8. TestSupport

- [ ] 8.1 `tests/QscDspDevices.TestSupport/FakeQrcServer.cs` — TCP listener implementing the QRC protocol per `research/QRC_PROTOCOL.md`.
- [ ] 8.2 Failure-injection knobs: `DropConnection()`, `DelayResponseMs(int)`, `EmitMalformed()`, `RespondWithStandbyError()`, `WithdrawKeepaliveTolerance()`.
- [ ] 8.3 `tests/QscDspDevices.TestSupport/DeterministicClock.cs` — `IQrcClock` impl with manual time advance.
- [ ] 8.4 `tests/QscDspDevices.TestSupport/TestLoggerSink.cs` — captures `Logger.Error/Warn/Notice/Debug` calls in-memory for assertions.

## 9. Unit tests

- [ ] 9.1 `QrcFramerTests` — round-trip, fragmentation across reads, multiple frames per buffer, oversize rejection.
- [ ] 9.2 `JsonRpcDispatcherTests` — id correlation happy path, AutoPoll-id reuse routes to subscription, notifications fire event, unknown-id logs warn.
- [ ] 9.3 `CommandQueueTests` — refuses while not accepting, FIFO under sequential, drain-and-flip on disconnect, saturation drops oldest.
- [ ] 9.4 `KeepaliveTimerTests` — emits NoOp after 30s silence, resets on outbound write (deterministic clock).
- [ ] 9.5 `ConnectionManagerTests` — every state transition, IsOnline-before-NotifyOnlineStatus invariant, queue cleared on disconnect.
- [ ] 9.6 `ReconnectStrategyTests` — exact 15s interval (deterministic clock), loops until Disconnect, no thread leak.
- [ ] 9.7 `ThreadCensusTests` — registers + unregisters cleanly, breach trips guard.
- [ ] 9.8 `BasicTcpClientTransportTests` — Moq-mocked BasicTcpClient; verifies events wired correctly without invoking the stub.

## 10. Property tests (FsCheck)

- [ ] 10.1 `QrcFramerProperties` — random byte streams produce no exceptions other than documented `FrameTooLargeException` and JSON-RPC parse errors. Round-trip property: encode then decode any valid request returns the original.
- [ ] 10.2 `CommandQueueProperties` — under random concurrent producers, dequeued order matches enqueue order across single producer; monotonic timestamps preserved.

## 11. Integration tests (against FakeQrcServer)

- [ ] 11.1 `Connect_succeeds_and_drives_IsOnline_then_NotifyOnlineStatus`.
- [ ] 11.2 `Connect_failure_logs_error_and_retries_after_15s` (deterministic clock).
- [ ] 11.3 `Mid_flight_socket_drop_triggers_15s_reconnect_loop_until_success`.
- [ ] 11.4 `Disconnect_drains_command_queue_and_joins_session_threads`.
- [ ] 11.5 `Outbound_silence_30s_emits_NoOp`.
- [ ] 11.6 `Standby_error_-32604_logs_warn_and_does_not_retry_command` (proper failover behaviour comes in M6; for M2 we just classify it correctly).
- [ ] 11.7 `Malformed_server_frame_logs_error_and_drops_connection`.
- [ ] 11.8 `Refuse_send_while_disconnected_logs_error_and_returns_silently`.

## 12. Documentation

- [ ] 12.1 Update `ARCHITECTURE.md` with the actual state-machine diagram, lock-order specifics, ThreadCensus details (replace the M1 placeholders with real text + file:line references).
- [ ] 12.2 Update `SPEC_COMPLIANCE.md` rows the milestone discharges (3.2-3.4, 4.1, 4.3, 7.1-7.4, 8.1-8.5) from ⏳ to ✅ with file:line citations.

## 13. Build, format, and review gates

- [ ] 13.1 `dotnet build QscDspDevices.sln`: 0 warnings, 0 errors (Debug + Release).
- [ ] 13.2 `dotnet format QscDspDevices.sln --verify-no-changes`: clean.
- [ ] 13.3 `dotnet test QscDspDevices.sln`: all tests pass.
- [ ] 13.4 Coverage on `QscDspDevices.dll`: ≥ 90% line coverage (CI gate).
- [ ] 13.5 DLL size (`-c Release`): ≤ 500 KB.
- [ ] 13.6 `openspec validate add-qrc-client-and-connection --strict`: passes.
- [ ] 13.7 Run qsc-critic agent locally; save report to `openspec/changes/add-qrc-client-and-connection/REVIEW.md`. Address blockers before opening the PR.

## 14. Commit + PR

- [ ] 14.1 Commit incrementally — one logical commit per major component (framer, dispatcher, queue, etc.) so the merge log reads as a build-up.
- [ ] 14.2 Open PR #2 with the spec-id reference and compliance citations per `.github/PULL_REQUEST_TEMPLATE.md`. (Push + PR creation gated by user approval — Claude will not push without explicit OK.)
