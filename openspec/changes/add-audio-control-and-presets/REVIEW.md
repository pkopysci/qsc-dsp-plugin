# Review — add-audio-control-and-presets

## Pass 1

**Verdict:** ⚠️ ship with caveats

**Quality gates (re-run locally):** Release build `-warnaserror` 0/0; `dotnet test` 245/245 across 3 consecutive runs (no flakes); coverage 91.1 % line / 80.6 % branch / 95.2 % method on `QscDspDevices.dll`; Release DLL 86,016 B / 500 KB.

### Blockers

1. **Task 5.2 is incorrectly checked: `RedactingDebugFormatter` is dead code.** (`Connectivity/ConnectionManager.cs:480`, `Protocol/Logging/RedactingDebugFormatter.cs`) The formatter is unit-tested in isolation but never called from production. `RunSendLoopAsync` calls `JsonConvert.SerializeObject(request)` straight onto the wire and emits no `Logger.Debug`. The only `Log.Debug` definition (`Plugin/Log.cs:43`) has zero call sites in `src/`. M2 critic deferred this to M3; M3 ships the formatter without wiring it. Either flip 5.2 back to `[ ]` and document an M7 wiring plan, or wire `Log.Debug(_deviceId, RedactingDebugFormatter.Format(request))` into `RunSendLoopAsync` before merging.

2. **`_autoPolls` dictionary leaks on every reconnect.** (`PostConnect/HydrateChangeGroupAction.cs:118`, `Protocol/JsonRpcDispatcher.cs:134`) Each successful hydration calls `RegisterAutoPoll`. On a transport fault, `CleanupAfterDisconnect` cancels pending requests but never calls `UnregisterAutoPoll` for the prior id; the next hydration registers a fresh id alongside the stale one. Over a long-running session with periodic blips this is unbounded. Worse, `RegisterAutoPoll` throws on duplicate ids — `IdGenerator` is monotonic so collision is unlikely soon, but a wrap or future generator reset surfaces this as a CA1031-swallowed Warn. Fix: snapshot live AutoPoll ids in `CleanupAfterDisconnect` and unregister, or expose `JsonRpcDispatcher.ClearAutoPolls()`.

### Concerns

1. **`AudioControlService.SetLevel`/`SetMute` update the cache before checking `TryEnqueue`.** (`AudioControl/AudioControlService.cs:103-107`, `:133-134`) When `Disconnected`, the queue refuses, but `GetLevel` immediately returns the optimistic value — framework UI shows a level the Core never received, with no replay-on-reconnect (hydration only re-subscribes). This contradicts the README's "refuse to queue/send commands while disconnected": from the framework's perspective the write is silently accepted. Either (a) only update cache when `TryEnqueue` returned true, or (b) document this as deliberate "intent" semantics in the XML doc and re-issue cached intents on reconnect. Currently neither is true.

2. **`LogonAction.WaitForCompletionAsync` has a narrow race on overlapping reconnects.** (`PostConnect/LogonAction.cs:85-89`) `RunAsync` writes `_completion` on entry. If reconnect N+1 enters `RunAsync` while hydrate from N is still spinning up, hydrate may observe N+1's fresh (incomplete) TCS and block on it. Composite serialises within a session, so this only fires across overlapping sessions, but Dispose+Connect during fault recovery is not impossible. Pin per-RunAsync TCS into the chain or take a session token.

3. **§4.5/4.6 deferral rationale is thin.** Tasks.md says registering send/keepalive with `ThreadCensus` "would breach the 3-cap." Counting: 1 session + 1 send + 1 keepalive = exactly 3, which fits; the receive path is event-driven and shouldn't count. The actual blocker is `RunSessionAsync` itself — once you remove it (per 4.5), the registrations slot in. As shipped, M3 has zero runtime enforcement on its M3-introduced Tasks; only the session task is registered. Either do §4.5/4.6 now or downgrade `ThreadCensus` claims in `SPEC_COMPLIANCE.md` from "runtime guard" to "session-task guard."

4. **`ChangeGroupManager.HandleAutoPollPush` silently swallows error responses on the AutoPoll id.** (`Protocol/ChangeGroup/ChangeGroupManager.cs:260-263`) If the Core errors the AutoPoll subscribe (error 5, or -32604 standby), `response.Result` is null, the method returns, and the manager continues to believe the group is subscribed. A subsequent `Set*` succeeds locally without cache-side reconciliation. Add an `OnError` path on `IAutoPollSubscription`.

5. **`HydrateChangeGroupAction` waits on `LogonAction.WaitForCompletionAsync` but the contract is untested.** Composite calls them sequentially, so the wait resolves immediately — redundant by accident. A future refactor that parallelises composite would silently break ordering. Add a unit test that pins it.

### Nits

- `AudioControlService.ExtractLevel` calls `_registry.TryGetChannelIdByTag` a second time even though `OnDeviceUpdate` already resolved it; pass the channel in.
- `RedactingDebugFormatter.Format`: comment at line 54 mis-describes the path — the rebuild only applies to Logon.
- Integration tests' `WaitForFrameCountAsync` / `WaitForStateAsync` spin with `Task.Delay(20)` and a 5 s wall-clock deadline. An event-based signal would protect against slow-CI false-fails.
- `LogonAction.RunAsync` line 156's `when ex is not OperationCanceledException` guard is redundant — line 151 already handles OCE.

### Praise

- `AudioChannelRegistry`'s reverse-lookup table (`_tagToChannelId`) plus explicit removal of stale entries on re-registration (lines 197-200) prevents "phantom delta" bugs after a config refresh.
- `RedactingDebugFormatter` defensively redacts both `Password` and `password` casings; once wired, the redaction is solid.
- `IntegrationEnv` composes the entire production stack with `RawTcpTransport` rather than mocking `IConnectionTransport` — closest thing to test-on-hardware the bet allows; the 4 integration tests assert on wire-format JSON.

### What I did NOT verify

- Mutation testing (no Stryker run).
- Behaviour on a real Q-SYS Core; all assertions are against `FakeQrcServer`.
- Long-running stability (the `_autoPolls` leak is reasoned, not measured).
- ECP backend (untouched by M3).
- `dotnet format --verify-no-changes` — assumed from task notes.
