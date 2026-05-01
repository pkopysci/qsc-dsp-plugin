# Tasks — add-audio-control-and-presets

## 1. Audio control core

- [ ] 1.1 `AudioControl/AudioChannelRegistry.cs` — thread-safe dict keyed by channel id; records `(levelTag, muteTag, levelMin, levelMax, isInput, routerIndex, bankIndex, tags)`. `RegisterInput`, `RegisterOutput`, `RegisterPreset`, `TryGetChannel`, `TryGetPreset`, `GetInputIds`, `GetOutputIds`, `GetPresetIds`. Re-registering the same id replaces the prior entry and logs `Logger.Notice`.
- [ ] 1.2 `AudioControl/LevelScaler.cs` — pure utility class. `ToDevice(int framework, int min, int max)`, `ToFramework(double device, int min, int max)`. Half-up rounding. Out-of-range framework input clamps and logs `Logger.Warn` once per offending caller (HashSet of warned ids).
- [ ] 1.3 `AudioControl/AudioControlService.cs` — owns the channel cache (`int level` and `bool mute` per channel). `SetInputLevel`/`SetOutputLevel`/`SetInputMute`/`SetOutputMute` enqueue the matching `Control.Set` to the dispatcher and update the cache optimistically. `Get*` returns from cache. `OnDeviceUpdate(string controlName, JToken value)` is called by the change-group manager and updates the cache + raises the four `IAudioControl` events when the cached value changes.
- [ ] 1.4 `AudioControl/PresetService.cs` — `RecallPreset(id)` looks up `(bank, index)` from the registry, enqueues `Snapshot.Load`. Logs `Logger.Notice` on response, `Logger.Error` on error.

## 2. Change-group + AutoPoll

- [ ] 2.1 `Protocol/ChangeGroup/ChangeGroupManager.cs` — owns the single `qsc-plugin-state` change group. `AddControl(name)`/`Destroy()`/`Subscribe(autoPollMs)`. Subscriptions accumulate; `Subscribe` is the call that issues `ChangeGroup.AutoPoll`.
- [ ] 2.2 Parse AutoPoll responses (`{ Result: { Id, Changes: [{ Name, Value, String?, Position? }] } }`) into `(string controlName, JToken value)` callbacks routed to `AudioControlService.OnDeviceUpdate`.
- [ ] 2.3 On `Disconnecting` issue `ChangeGroup.Destroy` synchronously on the receive thread before quiescing.
- [ ] 2.4 Hard cap of 4 change groups per connection enforced at `AddControl` time (logs `Logger.Error` and refuses; M3 only ever uses 1, but the cap exists for higher milestones).

## 3. Connection-manager extensions

- [ ] 3.1 `Connectivity/PostConnectActions/LogonAction.cs` — implements `IPostConnectAction` from M2. Reads username/password via supplied callback; sends `Logon` request; waits for response. Skips when both fields are empty.
- [ ] 3.2 `Connectivity/PostConnectActions/HydrateChangeGroupAction.cs` — registers every level/mute control from the channel registry into the change group, then calls `Subscribe(250)`.
- [ ] 3.3 Wire both actions into the existing `_postConnect` list in `ConnectionManager` in the order: Logon → HydrateChangeGroup. The HydrateChangeGroup action MUST wait on the Logon response (when present) before issuing its first subscribe.
- [ ] 3.4 On every reconnect the manager replays both actions; the change group is recreated from scratch.

## 4. 3-thread runtime

- [ ] 4.1 `Plugin/Threading/PluginThreads.cs` — owns three named `Thread` instances: `qsc-send`, `qsc-recv`, `qsc-timer`. Each calls `_threadCensus.Register(role)` on entry and disposes the registration on exit. Surface API: `Start()` / `RequestStop()` / `Join(TimeSpan)`.
- [ ] 4.2 Send thread: drains `CommandQueue` via `Channel<>.Reader.WaitToReadAsync` (run on a dedicated thread via `Task.WhenAny` over a `_stop` cancellation token).
- [ ] 4.3 Receive thread: owns the `_transport.RxBytesReceived` event handler and the `QrcFramer` byte buffer. Pushes complete frames into `JsonRpcDispatcher` synchronously.
- [ ] 4.4 Timer thread: hosts the existing `KeepaliveTimer` (30 s NoOp) and the reconnect interval. Replaces M2's `RunSessionAsync` schedule.
- [ ] 4.5 Remove `RunSessionAsync` from `ConnectionManager`. `Connect()` starts the trio synchronously; `Disconnect()` signals stop and joins each thread with a 5 s deadline (matches M2's existing dispose-time deadline).
- [ ] 4.6 `ThreadCensus` count reaches 3 in steady-state Connected; reaches 0 after Disconnect joins.

## 5. Logon redaction

- [ ] 5.1 `Protocol/Logging/RedactingDebugFormatter.cs` — formats a `JsonRpcRequest` for `Logger.Debug` with `Password` replaced by `"***"` only when the method is `"Logon"`. Other requests are formatted verbatim. Pins the redacting behaviour with property tests over arbitrary request shapes.
- [ ] 5.2 Wire the formatter into the framer's `Logger.Debug` path; production `Logger.Notice/Warn/Error` paths never see the body, so no other change is needed.

## 6. QscDspTcp surface

- [ ] 6.1 Implement the 12 `IAudioControl` methods on `QscDspTcp`. Each method delegates to `AudioControlService` / `PresetService`. Unknown id: `Get*` returns `0` / `false`; `Set*` logs `Logger.Error` and returns silently.
- [ ] 6.2 Wire the four `IAudioControl` events: `AudioInputLevelChanged`, `AudioInputMuteChanged`, `AudioOutputLevelChanged`, `AudioOutputMuteChanged`. Raised by `AudioControlService` via the existing `BaseDevice` event-dispatch convention.
- [ ] 6.3 `AddInputChannel` / `AddOutputChannel` / `AddPreset` register into `AudioChannelRegistry`. If called while already Connected, the new control is added to the existing change group (`ChangeGroupManager.AddControl(name)` then a one-shot `ChangeGroup.Poll` to seed the cache).

## 7. Tests — unit (xUnit + Moq + FsCheck)

- [ ] 7.1 `AudioChannelRegistryTests` — register/replace/lookup happy paths; concurrent-add property test.
- [ ] 7.2 `LevelScalerTests` — happy path table + FsCheck round-trip property over the full int range.
- [ ] 7.3 `AudioControlServiceTests` — `Set*` enqueues correct `Control.Set`; `Get*` returns from cache; cache update via `OnDeviceUpdate` raises the right event; unknown id → log error and silent return.
- [ ] 7.4 `PresetServiceTests` — recall enqueues `Snapshot.Load` with `(Name=bank, Bank=index)`; unknown id logs error.
- [ ] 7.5 `ChangeGroupManagerTests` — subscribe builds the right JSON-RPC requests; Destroy clears state; AutoPoll parse routes deltas to the registered callback; cap-at-4-groups guard.
- [ ] 7.6 `LogonActionTests` — empty creds ⇒ skip; populated creds ⇒ Logon sent; error response ⇒ logs warn and continues (M3 deferred behaviour: do not retry; M6 might).
- [ ] 7.7 `RedactingDebugFormatterTests` — `Logon` payload redacted; non-Logon untouched.
- [ ] 7.8 `PluginThreadsTests` — three threads start, register with census, stop on signal, join within deadline.

## 8. Tests — integration (xUnit + FakeQrcServer + RawTcpTransport)

- [ ] 8.1 `Connect_with_credentials_sends_Logon_then_subscribe`.
- [ ] 8.2 `SetAudioInputLevel_round_trips_via_Control_Set` (asserts on the wire-format JSON the FakeQrcServer received).
- [ ] 8.3 `Server_pushed_AutoPoll_delta_fires_AudioInputMuteChanged`.
- [ ] 8.4 `Reconnect_re_subscribes_change_group_with_same_controls`.
- [ ] 8.5 `RecallAudioPreset_sends_Snapshot_Load_with_correct_bank_and_index`.
- [ ] 8.6 `Logon_required_error_logs_warn_and_skips_subscribe` (we did not configure creds; the server demands them).

## 9. FakeQrcServer extensions

- [ ] 9.1 Implement `Control.Set` / `Control.Get` echo handlers in the FakeQrcServer (M2 only stubbed `NoOp`/`Logon`/`StatusGet`).
- [ ] 9.2 Implement `Snapshot.Load` echo handler.
- [ ] 9.3 Implement `ChangeGroup.AddControl` / `ChangeGroup.AutoPoll` / `ChangeGroup.Destroy` plus a deterministic AutoPoll burst trigger (`PushDelta(controlName, value)`).

## 10. Documentation

- [ ] 10.1 Update `ARCHITECTURE.md`: replace the "M2 ships one threadpool task; M3 will introduce dedicated send/receive/timer threads" paragraph with the actual layout from §D-5; add the post-connect-action list and change-group lifecycle.
- [ ] 10.2 Update `SPEC_COMPLIANCE.md`: discharge the README rows for level (3 rows), mute (3 rows), preset (1 row), the four audio events (4 rows), 3-thread budget (1 row), Logon credential handling (1 row).

## 11. Build, format, and review gates

- [ ] 11.1 `dotnet build`: 0 warnings, 0 errors (Debug + Release).
- [ ] 11.2 `dotnet format --verify-no-changes`: clean.
- [ ] 11.3 `dotnet test`: full matrix green, 3 consecutive runs, no flakes.
- [ ] 11.4 Coverage on `QscDspDevices.dll`: ≥ 90 % line.
- [ ] 11.5 DLL size (`-c Release`): ≤ 500 KB.
- [ ] 11.6 `openspec validate add-audio-control-and-presets --strict`: passes.
- [ ] 11.7 Run `qsc-critic` agent locally; save report to this change's `REVIEW.md`. Address blockers before opening the PR.

## 12. Commit + PR

- [ ] 12.1 Commit incrementally — one logical commit per major component (registry, scaler, service, change-group, post-connect actions, threads, redaction, tests, docs).
- [ ] 12.2 Open PR against `main` with full quality-gate dump and pass-3 critic verdict in the description. Push + PR creation gated by user approval.
