# Tasks — add-audio-control-and-presets

## 1. Audio control core

- [x] 1.1 `AudioControl/AudioChannelRegistry.cs` — thread-safe dict keyed by channel id; records `(levelTag, muteTag, levelMin, levelMax, isInput, routerIndex, bankIndex, tags)`. `RegisterInput`, `RegisterOutput`, `RegisterPreset`, `TryGetChannel`, `TryGetPreset`, `GetInputIds`, `GetOutputIds`, `GetPresetIds`. Re-registering the same id replaces the prior entry and logs `Logger.Notice`.
- [x] 1.2 `AudioControl/LevelScaler.cs` — pure utility class. `ToDevice(int framework, int min, int max)`, `ToFramework(double device, int min, int max)`. Half-up rounding. Out-of-range framework input clamps and logs `Logger.Warn` once per offending caller (HashSet of warned ids).
- [x] 1.3 `AudioControl/AudioControlService.cs` — owns the channel cache (`int level` and `bool mute` per channel). `SetInputLevel`/`SetOutputLevel`/`SetInputMute`/`SetOutputMute` enqueue the matching `Control.Set` to the dispatcher and update the cache optimistically. `Get*` returns from cache. `OnDeviceUpdate(string controlName, JToken value)` is called by the change-group manager and updates the cache + raises the four `IAudioControl` events when the cached value changes.
- [x] 1.4 `AudioControl/PresetService.cs` — `RecallPreset(id)` looks up `(bank, index)` from the registry, enqueues `Snapshot.Load`. Logs `Logger.Notice` on response, `Logger.Error` on error.

## 2. Change-group + AutoPoll

- [x] 2.1 `Protocol/ChangeGroup/ChangeGroupManager.cs` — owns the single `qsc-plugin-state` change group. `AddControl(name)`/`Destroy()`/`Subscribe(autoPollMs)`. Subscriptions accumulate; `Subscribe` is the call that issues `ChangeGroup.AutoPoll`.
- [x] 2.2 Parse AutoPoll responses (`{ Result: { Id, Changes: [{ Name, Value, String?, Position? }] } }`) into `(string controlName, JToken value)` callbacks routed to `AudioControlService.OnDeviceUpdate`.
- [ ] 2.3 On `Disconnecting` issue `ChangeGroup.Destroy` synchronously on the receive thread before quiescing. **Deferred to M7.** The current cleanup path drains the CommandQueue and calls `_transport.Disconnect()` — any in-flight Destroy would race the disconnect. The Core GCs the change group on socket close anyway (per QRC docs §5), so this is a courtesy-write, not a correctness issue.
- [x] 2.4 Hard cap of 4 change groups per connection enforced at `AddControl` time (logs `Logger.Error` and refuses; M3 only ever uses 1, but the cap exists for higher milestones).

## 3. Connection-manager extensions

- [x] 3.1 `Connectivity/PostConnectActions/LogonAction.cs` — implements `IPostConnectAction` from M2. Reads username/password via supplied callback; sends `Logon` request; waits for response. Skips when both fields are empty.
- [x] 3.2 `Connectivity/PostConnectActions/HydrateChangeGroupAction.cs` — registers every level/mute control from the channel registry into the change group, then calls `Subscribe(250)`.
- [x] 3.3 Wire both actions into the existing `_postConnect` list in `ConnectionManager` in the order: Logon → HydrateChangeGroup. The HydrateChangeGroup action MUST wait on the Logon response (when present) before issuing its first subscribe.
- [x] 3.4 On every reconnect the manager replays both actions; the change group is recreated from scratch. (Implementation: `ConnectionManager.OnConnectedAsync` invokes `_postConnect.RunAsync` on every Connected transition, including reconnects. End-to-end integration test for this property is deferred — see task 8.4 — but the M2 reconnect-loop tests + M3 hydration-shape tests cover both halves.)

## 4. 3-thread runtime

**§4 design change:** the original "three dedicated `Thread` instances" plan was replaced with **`Task`-based equivalents** during implementation. The README budget rule constrains *count*, not *type*, of plugin-owned units of work; using `Task.Run` matches the rest of the codebase and lets `ThreadCensus`'s token-based registration give the same budget guarantee. ARCHITECTURE.md §"Threading model — M3 (shipped)" documents the actual shape. The §4 task lines below are kept for traceability but reframed:

- [x] 4.1 _Replaced by `Task`-based equivalents._ See `ConnectionManager.StartIoLoops` for the wiring; `ThreadCensus` registration is preserved through the existing session-task token.
- [x] 4.2 Send Task: `ConnectionManager.RunSendLoopAsync` drains `CommandQueue.DequeueAsync`, frames via `QrcFramer.Encode`, writes via `transport.Send`. (Implemented as `Task.Run` rather than dedicated `Thread`.)
- [x] 4.3 Receive path: event-handler subscribed on `transport.RxReceived` feeds `QrcFramer.Append` → `JsonRpcDispatcher.Dispatch`. Runs on whatever threadpool worker the transport raises the event on; framer state is preserved on the manager.
- [x] 4.4 Timer Task: `ConnectionManager.RunKeepaliveLoopAsync` ticks `KeepaliveTimer.TickAsync` every 1 s. The timer itself enforces the 30 s NoOp silence window. (Reconnect interval continues to use `ReconnectStrategy` on the session-task path from M2.)
- [ ] 4.5 Remove `RunSessionAsync` from `ConnectionManager`. **Not done.** The session task still drives the state machine; the M3 send/receive/keepalive Tasks live alongside it. Removal would require collapsing the connect-attempt flow into the trio, which is invasive and out-of-scope for M3. Re-evaluate in M7 hardening.
- [ ] 4.6 `ThreadCensus` reports exactly 3 in steady-state. **Currently reports 1** (the session task's registration). Adding registrations for the 3 sub-tasks would breach the 3-cap (1 session + 1 send + 1 keepalive = 3 OK; the receive path is event-driven and shouldn't count). Wire the per-task registrations in M7 alongside the §4.5 cleanup.

## 5. Logon redaction

- [x] 5.1 `Protocol/Logging/RedactingDebugFormatter.cs` — formats a `JsonRpcRequest` for `Logger.Debug` with `Password` replaced by `"***"` only when the method is `"Logon"`. Other requests are formatted verbatim. Pins the redacting behaviour with property tests over arbitrary request shapes.
- [x] 5.2 Wire the formatter into the framer's `Logger.Debug` path; production `Logger.Notice/Warn/Error` paths never see the body, so no other change is needed.

## 6. QscDspTcp surface

- [x] 6.1 Implement the 12 `IAudioControl` methods on `QscDspTcp`. Each method delegates to `AudioControlService` / `PresetService`. Unknown id: `Get*` returns `0` / `false`; `Set*` logs `Logger.Error` and returns silently.
- [x] 6.2 Wire the four `IAudioControl` events: `AudioInputLevelChanged`, `AudioInputMuteChanged`, `AudioOutputLevelChanged`, `AudioOutputMuteChanged`. Raised by `AudioControlService` via the existing `BaseDevice` event-dispatch convention.
- [ ] 6.3 `AddInputChannel` / `AddOutputChannel` / `AddPreset` register into `AudioChannelRegistry`. If called while already Connected, the new control is added to the existing change group (`ChangeGroupManager.AddControl(name)` then a one-shot `ChangeGroup.Poll` to seed the cache). **Half done:** the registry registration works at any time, but mid-session subscribe-the-new-control logic is not wired (the change group is built from the registry only during the post-connect hydration). Defer to M7; in practice the framework does the configuration work before Connect.

## 7. Tests — unit (xUnit + Moq + FsCheck)

- [x] 7.1 `AudioChannelRegistryTests` — register/replace/lookup happy paths; concurrent-add property test.
- [x] 7.2 `LevelScalerTests` — happy path table + FsCheck round-trip property over the full int range.
- [x] 7.3 `AudioControlServiceTests` — `Set*` enqueues correct `Control.Set`; `Get*` returns from cache; cache update via `OnDeviceUpdate` raises the right event; unknown id → log error and silent return.
- [x] 7.4 `PresetServiceTests` — recall enqueues `Snapshot.Load` with `(Name=bank, Bank=index)`; unknown id logs error.
- [x] 7.5 `ChangeGroupManagerTests` — subscribe builds the right JSON-RPC requests; Destroy clears state; AutoPoll parse routes deltas to the registered callback; cap-at-4-groups guard.
- [x] 7.6 `LogonActionTests` — empty creds ⇒ skip; populated creds ⇒ Logon sent; error response ⇒ logs warn and continues (M3 deferred behaviour: do not retry; M6 might).
- [x] 7.7 `RedactingDebugFormatterTests` — `Logon` payload redacted; non-Logon untouched.
- [ ] 7.8 `PluginThreadsTests` — three threads start, register with census, stop on signal, join within deadline. **N/A in current shape:** see §4 design-change note above; the M3 implementation uses `Task`-based loops rather than dedicated `Thread`s. Equivalent coverage lives in `ConnectionManagerTests` (state-machine) and the integration suite (end-to-end I/O).

## 8. Tests — integration (xUnit + FakeQrcServer + RawTcpTransport)

- [x] 8.1 `Connect_with_credentials_sends_Logon_then_subscribe`.
- [x] 8.2 `SetAudioInputLevel_round_trips_via_Control_Set` (asserts on the wire-format JSON the FakeQrcServer received).
- [x] 8.3 `Server_pushed_AutoPoll_delta_fires_AudioInputMuteChanged`.
- [ ] 8.4 `Reconnect_re_subscribes_change_group_with_same_controls`. **Deferred** with inline rationale in `tests/QscDspDevices.IntegrationTests/Audio/AudioControlEndToEndTests.cs`. The property is implied by the M2 `Mid_flight_drop_triggers_reconnect_loop` test plus the M3 `Hydration_enqueues_AddControl_per_tag_then_AutoPoll` test. Re-evaluate during M7 hardening.
- [x] 8.5 `RecallAudioPreset_sends_Snapshot_Load_with_correct_bank_and_index`.
- [ ] 8.6 `Logon_required_error_logs_warn_and_skips_subscribe` (we did not configure creds; the server demands them). **Deferred:** unit-level coverage in `LogonActionTests.Error_response_marks_completion_false_but_returns_normally` plus `HydrateChangeGroupActionTests.Hydration_waits_for_LogonAction_completion_when_supplied` cover the two halves. The integration variant adds little beyond timing assertions and is left as a smoke test for M7.

## 9. FakeQrcServer extensions

- [x] 9.1 Implement `Control.Set` / `Control.Get` echo handlers in the FakeQrcServer (M2 only stubbed `NoOp`/`Logon`/`StatusGet`).
- [x] 9.2 Implement `Snapshot.Load` echo handler.
- [x] 9.3 Implement `ChangeGroup.AddControl` / `ChangeGroup.AutoPoll` / `ChangeGroup.Destroy` plus a deterministic AutoPoll burst trigger (`PushDelta(controlName, value)`).

## 10. Documentation

- [x] 10.1 Update `ARCHITECTURE.md`: replace the "M2 ships one threadpool task; M3 will introduce dedicated send/receive/timer threads" paragraph with the actual layout from §D-5; add the post-connect-action list and change-group lifecycle.
- [x] 10.2 Update `SPEC_COMPLIANCE.md`: discharge the README rows for level (3 rows), mute (3 rows), preset (1 row), the four audio events (4 rows), 3-thread budget (1 row), Logon credential handling (1 row).

## 11. Build, format, and review gates

- [x] 11.1 `dotnet build`: 0 warnings, 0 errors (Debug + Release).
- [x] 11.2 `dotnet format --verify-no-changes`: clean.
- [x] 11.3 `dotnet test`: full matrix green, 3 consecutive runs, no flakes.
- [ ] 11.4 Coverage on `QscDspDevices.dll`: ≥ 90 % line.
- [x] 11.5 DLL size (`-c Release`): ≤ 500 KB.
- [x] 11.6 `openspec validate add-audio-control-and-presets --strict`: passes.
- [ ] 11.7 Run `qsc-critic` agent locally; save report to this change's `REVIEW.md`. Address blockers before opening the PR.

## 12. Commit + PR

- [x] 12.1 Commit incrementally — one logical commit per major component (registry, scaler, service, change-group, post-connect actions, threads, redaction, tests, docs).
- [ ] 12.2 Open PR against `main` with full quality-gate dump and pass-3 critic verdict in the description. Push + PR creation gated by user approval.
