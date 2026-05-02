# Critic Review — add-logic-triggers / milestone/m5-logic-triggers

## Pass 1

**Verdict: ⚠️ ship with caveats** — one Concern, three Nits, no blockers.

**Gates re-verified locally (HEAD 5a7cfbb):**
- `dotnet build -c Release /p:TreatWarningsAsErrors=true`: 0/0
- `dotnet test --no-build -c Release` x3 runs: 317/317 each, no flakes
- DLL size: 99,840 B / 500 KB
- Fresh cobertura on M5 surface:
  - `LogicTriggerRegistry`: 100% line / 100% branch
  - `LogicTriggerService`: 100% line / 90% branch
  - `AudioControlServiceFanout`: 100% line / 100% branch
  - `HydrateChangeGroupAction`: 88.6% line / 75% branch (uncovered branch is the M3 all-zero / queue-refused path, not M5-introduced)

### Blockers

None.

### Concerns

1. **Receive-thread back-pressure is real but only partially mitigated** (`ConnectionManager.cs:401-412`, `JsonRpcDispatcher.cs:198-202`, `ChangeGroupManager.cs:286-306`). The chain `framer → dispatcher.Dispatch → subscription.OnPush → fanout.Dispatch → LogicTriggerService.OnDeviceUpdate → LogicTriggerStateChanged?.Invoke` runs entirely on the rx thread inside one `try { foreach } catch (FrameTooLargeException)` block. **No path drops registered-tag deltas** (good — answers the brief), but a framework subscriber that throws on `DspLogicTriggerStateChanged` escapes the only catch and propagates into the `BasicTcpClient` rx event chain; one that blocks stalls the receive loop and starves the keepalive's response path. At 10 Hz the queue itself is fine — this is a misbehaved-handler problem, not a queue-depth problem — but a single bad consumer can wedge the connection. Suggested fix: wrap the `_dispatcher.Dispatch(frame)` call (or, narrower, the `callback(...)` invocation in `HandleAutoPollPush`) in a per-frame `try/catch (Exception ex) { Log.Error(...); }`. Cheap; can land in M6 if you'd rather not retouch M5.

### Nits

- **Trigger-vs-audio collision is silent** (`AudioChannelRegistry.cs:261-272`). M4's `WarnIfTagCollides` only inspects `_tagToChannelId`, so a tag claimed by both a trigger and an audio level passes silently — the trigger wins per documented precedence. The fanout test (`AudioControlServiceFanoutTests.cs:118-149`) pins precedence but does not assert "no warning was logged," and no symmetric warn exists on the trigger registry. Defensible (the precedence is documented), but a one-line `Log.Notice` from `LogicTriggerRegistry.Register` when the supplied `tagName` is already a known channel/zone tag would be cheap diagnostic value.
- **Pulse failure on Standby Core is invisible to the framework** (`LogicTriggerService.cs:81-89`, intentional per the brief). Matches the M3 mute-Set shape — fire-and-forget, dispatcher logs the error response, framework gets no signal. Correct for shape consistency; framework `IDspLogicTriggerSupport` has no failure-channel anyway. One-sentence callout in `design.md` would pre-empt the third-party reviewer's question.
- **`Pulse_with_null_id_throws` bypasses the `QscDspTcp` validation layer** (`LogicTriggerServiceTests.cs:96-103`). Both paths reject — `LogicTriggerService.Pulse` has `ArgumentNullException.ThrowIfNull(id)` (`:73`), and `QscDspTcp.PulseDspLogicTrigger` calls `ParameterValidator.ThrowIfNullOrEmpty` first (`:484`). Defence-in-depth is appropriate; the unit test exercises the inner contract, which is the right level. A one-line plugin-surface test asserting the same on `QscDspTcp` would close the loop.
- **Reconnect re-fires `LogicTriggerStateChanged` even when the pre-disconnect value was identical.** Acceptable per the cache-less design; momentary pulses are legitimately distinct fires. No code change. Worth a sentence in `design.md` so it isn't a surprise to a downstream consumer caching at their layer.

### Praise

- Cache-less design captured both in `LogicTriggerService.cs:18-28` and in a deliberate test (`OnDeviceUpdate_fires_event_on_every_delta_no_coalescing`) — the bug-detection signal that calibrates trust.
- Fanout precedence pinned by two tests including an explicit configuration-error scenario; precedence string documented in the class XML doc.
- `OnDeviceUpdate` defends against unregistered-tag calls even though the fanout filters upstream — belt-and-braces called out in the comment.
- Additive ctors on `HydrateChangeGroupAction` and `AudioControlServiceFanout` preserve M3/M4 call-sites without breaking changes.

### What I did NOT verify

- Did not run mutation testing on the new code paths.
- Did not exercise the rx-thread blocking scenario empirically; the Concern is from reading the call chain, not a load test.
- Did not validate against a real Q-SYS Core; FakeQrcServer is the only protocol-level oracle.
- Did not check whether `gcu_common_utils.Logging.Logger` can throw from within a handler invocation (would compound the Concern if so).
