# QSC Critic Review (Pass 2) — add-qrc-client-and-connection / milestone/m2-qrc-client-and-connection
**Date:** 2026-04-30 UTC
**Build:** green (Debug + Release, 0 warnings, 0 errors)
**Tests:** 143 nominal (131 unit + 4 integration + 8 property) — but **2 unit tests are flaky under parallel execution** (see Blocker 1). Repeated runs of the unit suite show 4/8 (50%) failure rate.
**Coverage:** 90.0% line / 84.5% branch / 94% method on QscDspDevices.dll (Cobertura summary).
**DLL size (Release):** 49,152 bytes (48 KB) / 500 KB budget
**Verdict:** ❌ block — a pass-1 fix introduced a non-trivial async/threadid bug that is observable as test flake.

## Status of pass-1 findings

| # | Item | Status |
|---|------|--------|
| B1 | ThreadCensus wired into production | partly — wiring exists, but uses `Environment.CurrentManagedThreadId` across `await` boundaries, which is unsound (see new B1 below) |
| B2 | tasks.md / ARCHITECTURE.md drift | resolved — task 7.3 deferred-to-M3 with rationale; task 9.8 backed by `tests/QscDspDevices.UnitTests/Transport/BasicTcpClientTransportTests.cs`; ARCHITECTURE.md §"Threading model" now describes M2 (one threadpool task) vs M3 (planned 3-thread layout) |
| B3 | Coverage 76.7% → 90.0% | resolved — `Summary.txt` reports 90.0% line, lowest class is `BasicTcpClientTransport` 81.8%; `BasicTcpClientTransportTests` lands 12 cases |
| C1 | Disconnecting / Disconnected ordering | resolved — `TransitionLocked` is gone; `Disconnect()` releases the lock before calling `TransitionTo` (`Connectivity/ConnectionManager.cs:147-171`). Both raises now synchronous |
| C2 | Internal types public, exposing `Task` surface | deliberately deferred to a follow-up PR per developer note — accepted |
| C3 | JsonRpcDispatcher CTR leak | resolved — new `PendingRequest` holder owns both TCS and `CancellationTokenRegistration`; disposal on cancel (`:101`), on completion (`:194`), and on `CancelAllPending` (`:218`); guarded by `CanBeCanceled` (`:94`) |
| C4 | CommandQueue saturation race | resolved — entire enqueue is under one `lock (_stateLock)` (`Protocol/CommandQueue.cs:174-207`) |
| C5 | JSON `MaxDepth` not pinned | resolved — `JsonSerializerSettings { MaxDepth = 32 }` static instance reused on every `DeserializeObject` (`Protocol/JsonRpcDispatcher.cs:42-45,155`) |
| C6 | `BasicTcpClientTransport.Dispose` clears events | resolved — `Connected = null;` etc. removed; comment added explaining why (`Transport/BasicTcpClientTransport.cs:108-118`) |
| C7 | Literal NUL byte in property test source | resolved — `file(1)` reports `Unicode text, UTF-8 text` |
| C8 | CS0067 pragma test | deferred to M3 design.md per developer note — accepted |

## Blockers (must fix before merge)

1. **`ThreadCensus.Register("session")` and `Unregister()` straddle an `await`, leaving the census permanently leaked when the continuation resumes on a different thread** (`src/QscDspDevices/Connectivity/ConnectionManager.cs:238` and `:273`; `src/QscDspDevices/Plugin/Threading/ThreadCensus.cs:76,109` use `Environment.CurrentManagedThreadId`).
   - Why it matters: `RunSessionAsync` is an `async` method scheduled via `Task.Run`. `Register` records the threadpool worker that started the method. After every `await` (`TryOneAttemptAsync`, `WaitForFaultOrCancellationAsync`, `WaitForNextAttemptAsync`), the continuation resumes on whatever threadpool thread is free. The `finally`-block `Unregister()` therefore runs on a *different* thread and removes that thread's id from `_alive` — but the original session-thread id is still in the set. The HashSet has 1 alive entry, the snapshot still contains "session", and `AliveCount` stays at 1 after the session task fully completes. This makes `Disconnect_releases_the_session_thread_back_to_zero` and `Connect_drives_state_through_Connecting_to_Connected` flaky in proportion to threadpool starvation: under serial execution they pass; running the full unit suite shows them fail in 4 of 8 attempts (`AliveCount` observed as 1, not 0; or `WaitForStateAsync` exceeds 2 s waiting on a starved Task.Run continuation). The deeper problem is that `ThreadCensus` was designed for OS threads pinned to a role (M3's planned send/receive/timer trio with their own `Thread` instances) but is being applied to an async method whose "thread" is a moving target.
   - Suggested fix: do not use `Environment.CurrentManagedThreadId` as the census key when the registered work is async. Two viable options:
     1. Register/Unregister at the *Task* level: `_threadCensus.Register("session", Task.CurrentId ?? -1)` plus a matching `Unregister(taskId)`, or use a `Guid` token returned by `Register`. The `RunSessionAsync` method then captures the token in a local before its first await and disposes it in `finally`.
     2. Defer registration to M3 when actual `Thread` instances exist for send/receive/timer. Until then, document that M2 has zero registered threads (the threadpool task is not a plugin-owned thread under any meaningful definition) and remove the wiring from `ConnectionManager`. The two flaky tests would be deleted alongside the spec-scenario claim that they enforce — which is honest, since the M2 spec scenarios "Steady-state plugin reports plugin threads" / "Disconnected plugin reports 0 plugin threads" can't be observed reliably while the registration is keyed on a transient thread id anyway.
   - Either fix unblocks merge; option 2 is smaller and concedes the M2 spec scenario was over-promised.

2. **Two unit tests are flaky under parallel execution** (`tests/QscDspDevices.UnitTests/Plugin/QscDspTcpTests.cs:354 Disconnect_releases_the_session_thread_back_to_zero` and `tests/QscDspDevices.UnitTests/Connectivity/ConnectionManagerTests.cs:50 Connect_drives_state_through_Connecting_to_Connected`)
   - Why it matters: 8 consecutive runs of `dotnet test tests/QscDspDevices.UnitTests/...csproj --no-build` under the standard xUnit parallelism produced 4 failures total. The first test fails because of Blocker 1; the second times out at 2 s waiting for `state == Connected` because `RunSessionAsync` (a `Task.Run` continuation) has not yet been scheduled on a starved threadpool. CI noise of this magnitude will burn through the third-party reviewer's patience and — worse — will eventually be silenced with a retry policy that hides real regressions.
   - Suggested fix: bump `WaitForStateAsync`'s default timeout to 5–10 s (current 2 s is the actual offender for the second test); replace the polling waits with event-driven `TaskCompletionSource` hooks on `StateChanged`; and resolve Blocker 1 to fix the first test.

## Concerns (should fix; not strictly blocking)

1. **Stub `BasicTcpClient.Connected` returns `false` unconditionally, masking any post-Connect behaviour the production transport relies on** (`src/FrameworkStubs/CommonUtils/NetComs/BasicTcpClient.cs:90`)
   - The pass-1 review didn't flag this; the relaxation from `throw NotImplementedException` to `=> false` was made to allow `BasicTcpClientTransport.IsConnected` and `Send`'s connectivity check to be exercised in unit tests. The justification comment is sound for the *current* tests, but the real `BasicTcpClient` returns `true` after a successful Connect. The transport's `Send` path (`Transport/BasicTcpClientTransport.cs:92-95`) gates on `!_client.Connected`, so any future test that calls `transport.Send(...)` after a stub-side success path will see `InvalidOperationException("Transport is not connected.")` — and the tester will conclude the bug is in `Send` rather than in the stub. Two of the new transport unit tests (`Send_when_not_connected_throws_InvalidOperationException`, `Send_after_Dispose_throws_ObjectDisposedException`) are tautologies under this stub: they pass for the wrong reason.
   - Suggested fix: drop `Send_when_not_connected_throws_InvalidOperationException` (it's a no-op against the stub) or move it to a Moq-driven test that injects a `BasicTcpClient` whose `Connected` property is true. Add a comment block at the top of `BasicTcpClientTransportTests` explaining what *is* being covered (constructor validation + dispose pattern) and what is NOT (the actual Connect/Send/Receive paths, which are deliberately routed through `RawTcpTransport` in the integration suite).

2. **`ConnectionManager.AsTask(CancellationToken)` still leaks a `CancellationTokenRegistration` per call** (`src/QscDspDevices/Connectivity/ConnectionManager.cs:224-229`)
   - Pass-1 nit, not addressed. The fix is the same shape as the JsonRpcDispatcher CTR fix (Concern 3): keep the `CTR` returned by `Register`, dispose it when the TCS resolves. As-is, every `RunSessionAsync` iteration that goes through `WaitForFaultOrCancellationAsync` allocates a CTR that lives for the whole session.

3. **`ConnectionManager.WaitForDisconnectedAsync` waits on `_sessionTask` but `_sessionTask` is set inside `Connect()` without volatility / locking** (`Connectivity/ConnectionManager.cs:131,180`)
   - The `Connect()` -> `Task.Run(...)` -> `_sessionTask = Task.Run(...)` assignment happens after the lock is released. A test that calls `WaitForDisconnectedAsync` immediately after `Connect()` may see `_sessionTask is null` if the assignment hasn't completed yet (rare but observable on a Crestron-class CPU). Either move the assignment under `_stateLock`, or have `Connect()` complete the assignment before returning by capturing the task before fire-and-forgetting.

## Nits (style, doc tweaks)

- `Plugin/QscDspTcp.cs:450-453`: empty `case ConnectionState.Connecting:` with comment is still present. Pass-1 nit; not addressed. Either drop the case (the default no-op covers it) or attach the rationale at the method level.
- `Plugin/QscDspTcp.cs:158-162`: doc still reads "M2 captures these for logging only" but the body discards them. Pass-1 nit; not addressed.
- `Connectivity/ConnectionManager.cs:208-218`: the `_sessionTask?.Wait(TimeSpan.FromSeconds(5))` swallows `AggregateException` and `TaskCanceledException` but lets every other exception escape `Dispose()` — including the `OperationCanceledException` from `_postConnect.RunAsync`. Disposing during a hot post-connect cycle would propagate that exception to the caller. Consider catching `OperationCanceledException` explicitly or wrapping in a single `catch when (ex is AggregateException or OperationCanceledException)`.
- `Protocol/JsonRpcDispatcher.cs:212`: `_pending.Keys.ToArray()` followed by `TryRemove` is functionally equivalent to the previous loop but the comment "ConcurrentDictionary tolerates this" is now stale — keep the new shape, drop the comment.
- The `OnStateChanged` switch in `QscDspTcp` treats `Disconnecting` identically to `Disconnected`, both setting `IsOnline = false` and calling `NotifyOnlineStatus`. With `TransitionTo` now firing both transitions synchronously, consumers will see *two* `IsOnline = false / NotifyOnlineStatus()` cycles per Disconnect. The framework `BaseDevice.NotifyOnlineStatus` may dedupe, but it's worth verifying — if not, suppress the second notification when `IsOnline` is already false.

## Praise

- The `PendingRequest` holder type pattern in `JsonRpcDispatcher` is the right shape: registration disposed on every exit path (cancel, complete, mass-cancel), with a fast-path `CanBeCanceled` guard that avoids the allocation entirely for the dominant "no token" call site. This is the pattern I'd expect from a mid-level systems engineer.
- The single-lock fix in `CommandQueue.TryEnqueue` is the boring correct answer. The earlier double-checked lock pattern was a premature-optimization smell; replacing it with a single critical section makes the saturation invariant easy to read and audit.
- `JsonRpcDispatcher`'s `MaxDepth = 32` constant carries a justification comment that explicitly names the threat model ("a transitive-dependency change cannot relax it behind our backs"). That's the right way to inline reviewer-context.

## What I did NOT verify

- I did not run the test suite under sustained load (e.g. 100+ iterations) to characterise the flakiness rate beyond the 8 runs sampled.
- I did not validate against a real Q-SYS Core or Designer emulator.
- The framework stub's behavioural fidelity to the real `gcu_common_utils.NetComs.BasicTcpClient` is unverified for both `Connected`-after-`Connect()` and event-firing semantics.
- I did not exhaust the `Task.CurrentId` alternative for ThreadCensus to confirm it survives `Task.Run + await` chains; the design needs a 5-minute spike before being adopted.
- Mutation testing remains deferred to M7.


---

# QSC Critic Review (Pass 3) — add-qrc-client-and-connection / milestone/m2-qrc-client-and-connection
**Date:** 2026-04-30 UTC
**Build:** Release `-warnaserror` 0/0; 7 consecutive `dotnet test -c Release` runs all green (135 unit + 4 integration + 8 property = 147/147 each).
**Coverage:** 90.0% line / 83.6% branch / 94.4% method on `QscDspDevices.dll`.
**DLL size (Release):** 49,664 B (49 KB) / 500 KB.
**Verdict:** ⚠️ ship with caveats — pass-2 blockers verified resolved; one new substantive concern; three carryovers.

## Status of pass-2 findings

| # | Item | Status |
|---|------|--------|
| B1 | ThreadCensus key tied to `Environment.CurrentManagedThreadId` across `await` | resolved — `Register` now returns a `ThreadCensusRegistration` struct holding an opaque `Interlocked.Increment` token (`Plugin/Threading/ThreadCensus.cs:86-109`, `ThreadCensusRegistration.cs:23-56`). Call site captures the handle before the first `await` and disposes in `finally` (`Connectivity/ConnectionManager.cs:241,276`). |
| B2 | Two flaky unit tests under parallel xUnit | resolved — `WaitForStateAsync` is event-driven on `StateChanged` with a 10s deadline (`tests/.../ConnectionManagerTests.cs:229-264`); `xunit.runner.json` sets `parallelizeTestCollections: false` with inline justification; `Disconnect_releases_the_session_thread_back_to_zero` uses `Dispose` as a synchronous join point before reading `AliveCount` (`tests/.../QscDspTcpTests.cs:347-354`). 7/7 clean. |
| C1 | Stub `BasicTcpClient.Connected => false` masks production behaviour | partial — clarifying comments added at `src/FrameworkStubs/.../BasicTcpClient.cs:82-90` and on `BasicTcpClientTransportTests` (line 9-19). `Send_when_not_connected_throws_InvalidOperationException` is still tautological under the stub but the test class summary documents this. Acceptable. |
| C2 | `AsTask(CancellationToken)` CTR leak | not addressed — `ConnectionManager.cs:224-229` unchanged. Per `git log` deferred to M3. Bounded leak (one CTR per session iteration); not a correctness bug, but real over deployment lifetimes. |
| C3 | `_sessionTask` assigned outside `_stateLock` | not addressed — `Connectivity/ConnectionManager.cs:131` unchanged; deferred to M3. `WaitForDisconnectedAsync` defends against `null` (`:181`) so the visible-symptom surface is small. Add a `// TODO(m3):` marker so the deferral is greppable. |

## Blockers

None.

## Concerns

1. **`ThreadCensus.Register` budget-breach behavior is inconsistent and arguably non-conforming in RELEASE** (`Plugin/Threading/ThreadCensus.cs:92-103`). DEBUG calls `Environment.FailFast` — that's an unconditional process termination; README §"Exception Handling" forbids host crashes. RELEASE returns a `Breach` sentinel and lets the offending unit of work proceed *untracked* — the budget guard becomes a counter, not a guard. Suggested fix: in both configurations log `Logger.Error`, refuse the registration, drop `FailFast`. Caller checks `registration.IsBudgetBreach` and short-circuits.

2. **`OnStateChanged` double-fires `IsOnline=false`/`NotifyOnlineStatus()` for `Disconnecting` then `Disconnected`** (`Plugin/QscDspTcp.cs:444-448`). Pass-2 nit-5; not addressed. Every user-initiated `Disconnect()` produces two `NotifyOnlineStatus()` calls in quick succession. Cannot verify whether `BaseDevice` dedupes without the real DLL. Cheapest defense: track `_lastNotifiedOnline` and skip when unchanged.

3. **`CommandQueue.SnapshotPending` is destructive but named like a peek** (`Protocol/CommandQueue.cs:226-235`). Reads via `_channel.Reader.TryRead`, removing items. XML doc says "Drains the queue synchronously" so it is documented honestly, but the name will mislead at every call site. Either rename to `DrainPendingForTest` or back it with an actual peek.

## Nits

- Pass-2 nits 1, 2, 4 (`QscDspTcp.cs:450-453` empty `Connecting` case; `:158-162` doc out of sync with `_ = ...` discards; `JsonRpcDispatcher.cs:212` stale "tolerates this" comment) — not addressed. 5-minute cleanup pass.
- `BasicTcpClientTransport` sits at 75% line coverage (lowest class). Moq tests of the event re-raise paths are the cheap win.
- `xunit.runner.json` adds a `"//"` key the referenced JSON Schema does not declare. Inert; xUnit ignores unknown keys.

## Praise

- `ThreadCensusRegistration` struct is the right shape: opaque token, `IDisposable`, sentinel via `default` so `IsBudgetBreach` is allocation-free. XML doc on `Register` explicitly names the async-continuation reason for avoiding thread ids.
- `xunit.runner.json` carries a paragraph-length justification for disabling test-collection parallelism that names the failure mode. Documentation-as-evidence.
- 7/7 green release runs, no warnings, no skips. The flake is gone.

## What I did NOT verify

- Stress runs beyond 7 iterations.
- Real `gcu_common_utils.NetComs.BasicTcpClient` post-Connect semantics.
- Whether `BaseDevice.NotifyOnlineStatus()` dedupes successive same-value calls (matters for Concern 2).
- Mutation testing — deferred per M2 plan.
- Round-trips against a real Q-SYS Core or Designer emulator.
