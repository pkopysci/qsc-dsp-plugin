# QSC Critic Review — add-qrc-client-and-connection / milestone/m2-qrc-client-and-connection
**Date:** 2026-04-30 UTC
**Build:** green (0 warnings, 0 errors, Debug + Release)
**Tests:** 111/111 passing (99 unit + 4 integration + 8 property), coverage **76.7%** on `QscDspDevices.dll` — below the project's 90 % target.
**DLL size (Release):** 49,152 bytes (48 KB) / 500 KB budget
**Verdict:** revise

## Blockers (must fix before merge)

1. **`ThreadCensus` is never wired into production code** (`src/QscDspDevices/Plugin/Threading/ThreadCensus.cs`, never referenced outside tests)
   - Why it matters: the active change spec (`specs/threading-budget/spec.md`, "Steady-state plugin reports 3 plugin threads" and "ThreadCensus runtime guard logs ... on a 4th thread") promises this guard is live. No production class instantiates `ThreadCensus`, none of `ConnectionManager`, `KeepaliveTimer`, or any session task calls `Register`/`Unregister`. The README §4 "max 3 threads" rule is therefore enforced by *convention only* in M2, even though the spec scenario asserts otherwise. A third-party reviewer running `grep -rn "new ThreadCensus" src/` will find zero hits and call this out.
   - Suggested fix: instantiate one `ThreadCensus` inside `QscDspTcp` (or `ConnectionManager`), and have any plugin-owned thread (today: the single `RunSessionAsync` task) register on entry and unregister on exit. Until M3 lands the dedicated send/receive/timer threads, at minimum register the session task so the spec scenario "Disconnected plugin reports 0 plugin threads" is observable.

2. **`tasks.md` and `ARCHITECTURE.md` claim files / tests that do not exist** (`openspec/changes/add-qrc-client-and-connection/tasks.md`, `ARCHITECTURE.md:73-77`)
   - **Task 7.3** "`Plugin/Threading/PluginTimer.cs` — single Task-driven timer thread shared by KeepaliveTimer + ReconnectStrategy" is `[x]` but the file does not exist (`find src -name PluginTimer.cs` returns nothing). `KeepaliveTimer:25-26` references "the shared timer thread inside `PluginTimer`", pointing at a class that was never written.
   - **Task 9.8** "`BasicTcpClientTransportTests` — Moq-mocked BasicTcpClient; verifies events wired correctly" is `[x]` but no such test file exists (`find tests -name "*BasicTcpClient*"` returns nothing). This is exactly the test that the design.md promises as the sole production-path coverage of `BasicTcpClientTransport`, which currently has **0 % line coverage**.
   - **`ARCHITECTURE.md:73-77`** lists `Protocol/SendLoop.cs`, `Protocol/ReceiveLoop.cs`, `Connectivity/PluginTimer.cs` as shipped in M2. None exist. `KeepaliveTimer` is implemented but never invoked from any session loop.
   - Why it matters: the audit story (tasks.md + ARCHITECTURE.md + design.md) is inconsistent with the codebase. Per the project rules ("Close the loop: tick `tasks.md` checkboxes ... in the same commit that delivers the work"), these items should be `[ ]` or the implementations should land before merge.
   - Suggested fix: revert the false `[x]` marks for 7.3 and 9.8, OR ship the missing files. Update ARCHITECTURE.md §"Threading model" to describe what M2 actually shipped (one async session task on the threadpool, no dedicated send/receive/timer threads) and clearly defer the three-thread layout to M3.

3. **Coverage on `QscDspDevices.dll` is 76.7 %, not 90 %** (line-rate from Cobertura, summed across the three test runs)
   - Per-file lowlights: `Transport/BasicTcpClientTransport.cs` 0 % (39 lines), `Plugin/QscDspTcp.cs` 63.6 % (132 lines), `Protocol/JsonRpc/JsonRpcError.cs` 0 % (3 lines), `Plugin/Threading/SystemClock.cs` 28.6 % (7 lines), `Protocol/FrameTooLargeException.cs` 50 % (default constructors untested).
   - Why it matters: the project sets a 90 % gate (`openspec/project.md` "≥ 90 % line coverage on QscDspDevices.dll") and CI is supposed to enforce it (task 13.4 — currently unchecked, accurately so). Shipping at 77 % means a regression in the un-tested 23 % is invisible to CI.
   - Suggested fix: add the missing `BasicTcpClientTransportTests` from task 9.8 (alone closes ~6 % of the gap), add tests for the M3-stub methods on `QscDspTcp` (zone enable, logic trigger, redundancy stubs are fully untested), exercise `SystemClock.DelayAsync` with a tiny real delay, and either suppress `FrameTooLargeException`'s parameterless / `(message)` / `(message,inner)` constructors with `[ExcludeFromCodeCoverage]` (they exist only to satisfy CA1032) or smoke-test them.

## Concerns (should fix; not strictly blocking)

1. **`ConnectionManager` raises `Disconnecting` via threadpool but `Disconnected` synchronously, allowing observers to see them out of order** (`src/QscDspDevices/Connectivity/ConnectionManager.cs:425-429` vs `:389-411`)
   - `TransitionLocked` (called from `Disconnect()` while holding `_stateLock`) does `ThreadPool.QueueUserWorkItem(_ => StateChanged?.Invoke(...))`. The session task then runs `FinishSession` -> `TransitionTo(Disconnected, ...)` which fires the event synchronously. There is no synchronization between the two, so a busy threadpool can yield `Disconnected -> Disconnecting`. The end-state `IsOnline=false` is correct in both orderings, so this is not a functional blocker — but it does violate the connection-manager spec's "State changes MUST be serialized so external observers never see an inconsistent snapshot."
   - Suggested fix: pick one strategy. Either fire both transitions synchronously (after dropping the lock — see `TransitionTo`'s shape, which already does that), or defer both via the same threadpool path so order is preserved. The threadpool detour was likely added to avoid re-entrancy; `TransitionTo` proves it isn't required if you release the lock before raising.

2. **Most internal-implementation classes are `public`, exposing `Task`-returning APIs the README forbids on the surface**
   - `ConnectionManager`, `CommandQueue`, `ReconnectStrategy`, `JsonRpcDispatcher`, `KeepaliveTimer`, `IPostConnectAction`, `BasicTcpClientTransport`, etc. are all `public sealed class`. They expose `Task DequeueAsync`, `ValueTask TickAsync`, `Task<JsonRpcResponse> RegisterPending`, `Task WaitForNextAttemptAsync`, `Task WaitForDisconnectedAsync`. README §4: "all threading must be managed internally (no public async/await)". The design.md narrows this rule to `QscDspTcp`'s framework-facing surface, but a strict reader of the README will count every public `Task` in the shipped DLL.
   - Why it matters: the third-party reviewer is auditing the public surface of `QscDspDevices.dll`, not just `QscDspTcp`. Every `Task`-returning public method is a finding under R5.
   - Suggested fix: add `[InternalsVisibleTo("QscDspDevices.UnitTests")]` / `IntegrationTests` / `PropertyTests` / `TestSupport` (in `AssemblyInfo` or via the csproj), then demote everything outside the framework contract (`QscDspTcp`, plus the `IConnectionTransport` and `IQrcClock` test seams that *deliberately* form a public DI surface) to `internal`.

3. **`JsonRpcDispatcher.RegisterPending` leaks `CancellationTokenRegistration` for any non-`None` token** (`src/QscDspDevices/Protocol/JsonRpcDispatcher.cs:68-74`)
   - The registration callback removes the entry from `_pending` on cancel, which is right, but the `CancellationTokenRegistration` returned by `Register` is discarded. For long-lived linked tokens (M3's session token plumbed through every command), this leaks one registration per RPC call. With AutoPoll lifetimes measured in hours and command rates in the hundreds-per-second, this is allocation pressure that compounds on the Crestron RMC4.
   - Suggested fix: store the registration alongside the TCS (e.g. `record Pending(TaskCompletionSource<JsonRpcResponse> Tcs, CancellationTokenRegistration Reg)`), and dispose the registration both on completion (`Dispatch` path) and on cancel. Alternatively, only register when the token can be cancelled (`if (cancellationToken.CanBeCanceled) ...`).

4. **`CommandQueue` saturation logic counts a "drop" even when a concurrent producer steals the freed slot** (`src/QscDspDevices/Protocol/CommandQueue.cs:185-219`)
   - Slow path: lock -> `TryRead(out _)` -> `Increment(_droppedTotal)` -> `TryWrite`. Between the TryRead and the TryWrite, another producer on the fast path can take the freed slot. Code logs "saturated and unable to enqueue even after dropping oldest" and increments the drop counter for a drop that didn't actually free room for *this* writer. The property test "Saturation drops oldest and increments drop counter" is sequential, so this race is invisible to it.
   - Why it matters: the design.md decision is "newer commands should win when the device can't keep up". Under concurrent saturation the counter is misleading and commands that *did* arrive at a free slot might be rejected.
   - Suggested fix: either move the entire fast-and-slow-path enqueue under the same lock (simpler, slightly more contention) or use `Channel<T>`'s built-in `BoundedChannelFullMode.DropOldest` and add a separate observer counter — the design.md explicitly rejected the built-in mode "because we want the noise" but you can still keep the noise by wrapping the writer.

5. **`Newtonsoft.Json` `MaxDepth` is left at the default rather than explicitly clamped** (`src/QscDspDevices/Protocol/JsonRpcDispatcher.cs:125`)
   - Newtonsoft's default `MaxDepth` is 64 since 13.0.1, which is fine, but the README says "must not crash the host". A future Newtonsoft upgrade or a `JsonSerializerSettings` regression could let a hostile peer's deeply-nested payload crash the receive loop. Pin the bound in source rather than in a transitive dependency.
   - Suggested fix: pass an explicit `JsonSerializerSettings { MaxDepth = 32 }` to `DeserializeObject`.

6. **`BasicTcpClientTransport.Dispose` nulls out events while subscribers may still hold references** (`src/QscDspDevices/Transport/BasicTcpClientTransport.cs:108-118`)
   - `Connected = null; ConnectionFailed = null; RxReceived = null;` is the C# 8+ "clear all subscribers" idiom but it does NOT free subscribers' delegate references (they still see the original delegate in their own state if they cached it). It also does nothing the `_client.RxBytesReceived -= OnRxBytesReceived` lines above don't already accomplish. No concrete crash today, but the pattern is fragile and confusing to a reader.
   - Suggested fix: keep the existing event-handler unsubscribes and remove the `Connected = null;` lines.

7. **A literal NUL byte (`0x00`) sits inside a comment in a source file** (`tests/QscDspDevices.PropertyTests/Protocol/QrcFramerProperties.cs:21`)
   - `xxd` confirms the comment "JSON escapes them as <NUL> anyway" actually contains a real `0x00` byte where `\u0000` was intended. This is why `file(1)` reports the source as `application/octet-stream`, why `grep` for keywords on this file silently misses, and why the `git diff` listing reports `Bin 0 -> 4118 bytes` instead of an additions count. Some build/diff/code-review tools choke on NUL in text files.
   - Suggested fix: replace the embedded NUL with the four-character escape `\u0000` (or just remove the byte).

8. **`IRedundancySupport` events are declared but suppressed via `#pragma warning disable CS0067`** (`src/QscDspDevices/Plugin/QscDspTcp.cs:91-119`)
   - Acceptable for M2 (events are raised in M3-M6), but the pragma will outlive its usefulness silently. Recommend a unit test that asserts each currently-unused event has zero subscribers raised in M2 so the day someone wires an event in M6 they can't accidentally leave the suppression on.

## Nits (style, doc tweaks)

- `Plugin/QscDspTcp.cs:142-146`: doc comment claims "M2 captures these for logging only" but `_ = coreId; _ = username; _ = password;` is the opposite of capture. Reword to "M2 discards these; M3 will capture them when Logon lands".
- `Plugin/QscDspTcp.cs:417-435`: `OnStateChanged` has an empty `case ConnectionState.Connecting:` with a comment. Either drop the case (the default no-op will cover it) or document why it's explicit at the method level.
- `Connectivity/ConnectionManager.cs:210-215`: `AsTask(CancellationToken)` allocates a TCS per call and registers a callback that is never disposed — same leak shape as concern 3.
- `Protocol/CommandQueue.cs:171-175`: log message "Command attempted while disconnected" is also fired when the queue is `_disposed`. Distinguish disposed-vs-disconnected so an operator can tell the plugin lost its session vs. a buggy caller held a reference past `Dispose()`.
- `Protocol/JsonRpcDispatcher.cs:178-184`: the loop `foreach ((long id, TCS) in _pending) { if (_pending.TryRemove(id, out _)) tcs.TrySetException(...); }` enumerates a `ConcurrentDictionary` while mutating it. Functional today (CD enumerators are snapshot-tolerant) but a co-reviewer will flag it; spell it as `_pending.Keys.ToList().ForEach(...)` or `_pending.Clear()` after capturing.
- `tests/QscDspDevices.IntegrationTests/Connection/FakeServerEndToEndTests.cs:140-149`: `WaitForStateAsync` polls with `Task.Delay(20)` and a 5 s deadline. On a busy CI box `Server_drop_triggers_state_back_into_Connecting_on_reconnect` could be flaky. Consider an event-driven wait (`TaskCompletionSource` subscribed to `StateChanged`).

## Praise

- The `ReconnectStrategy` is exactly 15 seconds, deterministic, no exponential backoff slipping in, and `ConnectionManagerTests.Failed_first_attempt_triggers_reconnect_after_exactly_fifteen_seconds` pins the boundary by advancing 14 s (no Connect) and then 1 s (Connect) — that is the right shape for a critic-resistant timing test.
- The `IsOnline` -> `NotifyOnlineStatus` ordering is end-to-end-tested in `QscDspTcpTests.Connect_drives_IsOnline_true_then_NotifyOnlineStatus` by reading `IsOnline` *inside* the `ConnectionChanged` handler. That is the right way to verify R3.
- The `FakeQrcServer` sends `EngineStatus` immediately on accept, frames every response with a single trailing `\x00`, and accepts a Logon with either `Password` or `Pin` — these match `research/QRC_PROTOCOL.md` §1.2, §8.1, and §3 closely enough that a real Q-SYS Core could be swapped in without touching the production code.
- `QrcFramer` correctly raises `FrameTooLargeException` on the projected size in `AppendToBuffer` (i.e. before allocating), not just after a write. That's the critical defense against the OOM-by-peer attack the README §"Exception Handling" implies.

## What I did NOT verify

- Real Q-SYS hardware behaviour against any of the protocol assumptions (no Core or Designer emulator was available).
- Mutation testing is not configured (deferred to M7 per scope).
- I did not inspect every test under load; flakiness only assessed by reading the wait helpers.
- I did not run the suite on Windows or against a Crestron RMC4. Coverage and timing on the target may differ from x86-64 Linux.
- The framework stub (`gcu_common_utils.NetComs.BasicTcpClient`) is itself unverified for behavioural fidelity to the real package; M2's only path through it is `BasicTcpClientTransport`, which has 0 % coverage.
- I did not exercise `RawTcpTransport` for IPv6 / loopback edge cases or partial-write scenarios; the integration tests only run the happy path on `127.0.0.1`.
