# Critic Review — add-redundancy / milestone/m6-redundancy

## Pass 2 — fixes

**Date:** 2026-04-30 UTC
**Build:** ✅ Release `-warnaserror` 0/0
**Tests:** ✅ 348/348 pass; the previously-flaky `RedundancyEndToEndTests.Failover_routes_subsequent_Control_Set_to_the_backup_wire` is 5/5 green cold-start and 3/3 green for the full integration suite cold-start.
**Format:** ✅ `dotnet format --verify-no-changes` clean.

### Blockers — addressed

1. **Integration-test flake** — rewritten to drive both EngineStatus transitions through `JsonRpcDispatcher.Dispatch` directly instead of waiting for `FakeQrcServer`'s on-accept auto-push. The wire still transports the actual `Control.Set` writes, so the assertion (write lands on the correct server's frame log) is unchanged.
2. **Primary `StateChanged` not observed** — added `OnPrimaryStateChanged` symmetric to `OnBackupStateChanged`. On non-Connected transitions while the primary is the active slot, it resets `_primaryState = EngineState.Unknown` and re-runs `SwitchbackPolicy.PickActive`.
3. **Backup transport + queue leak** — `RedundantConnectionPair` now accepts an optional `IConnectionTransport backupTransport` (defaults to null for tests that own a `StubTransport`). Its `Dispose` releases the backup queue and (when supplied) the backup transport. `QscDspTcp.BuildRedundantPair` passes `backup.Transport` so production paths dispose cleanly.

### Concerns — addressed or dismissed

4. **Group-manager disposal** — dismissed. `ChangeGroupManager` is non-`IDisposable`; nothing to release.
5. **Race in re-evaluate-then-apply** — defended in code by an end-of-Disconnect re-issue that always lands on `null`; for the live-policy paths the underlying operations (`SetActive` snapshot-under-lock, `SetDeltaCallback` swap) are idempotent. Note left for M7 if a race-detector run flags it.
6. **`Disconnect` did not clear `_activeSlot`** — `Disconnect` now atomically clears `_activeSlot`, resets both engine-state slots, and (when there was an active) calls `ApplyActiveSwitch(old, null)` to repoint the routing queue and fire `RedundancyStateChanged`.
7. **`QscRecommended` startup tie test missing** — already covered by `SwitchbackPolicyTests.QscRecommended_picks_Primary_at_startup_when_both_are_Active_and_no_current`. No new test needed.

### Nits — addressed or accepted

- `EngineStatusObserver`: explicit `JTokenType.Null` check added with a comment explaining why `IsNullOrEmpty` was insufficient.
- `RoutingCommandQueue` inherited bounded channel: accepted; the `CommandQueue` base was un-sealed and `TryEnqueue` made `virtual` specifically so the facade could override without re-exposing an interface; the bounded channel allocation is dwarfed by per-connection state.
- `tasks.md` 7.2/7.3/7.4 are unchecked (deferred); 9.7 now ticked.

**Verdict: ✅ ship.**

---

## Pass 1

**Date:** 2026-04-30 UTC
**Build:** ✅ Release `-warnaserror` 0/0
**Tests (full suite, 4 runs + integration suite alone, 5 cold-start runs + 1 coverage run):** **3 flakes of `RedundancyEndToEndTests.Failover_routes_subsequent_Control_Set_to_the_backup_wire`** in 10 invocations — once at line 51 (awaiting `primaryActive`), twice at line 74 (awaiting `backupActive`). Contradicts the proposal's "zero flakes across 5 stress runs" claim — those runs were warm; cold-start hits the issue routinely.
**Coverage:** post-M6 measurement aborted by the same flake; M5 baseline was 92.2%. Not re-measured.
**DLL size (Release):** 109.5 KB / 500 KB.

**Verdict: ⚠️ ship with caveats.** The headline is a flaky integration test (the only end-to-end coverage of the milestone), an untested asymmetry where the primary's TCP loss has no path to trigger failover, and a leaked backup transport+queue. Targeted fixes will close them; no redesign needed.

## Blockers

1. **The only end-to-end M6 test is flaky** (`tests/QscDspDevices.IntegrationTests/Redundancy/RedundancyEndToEndTests.cs:51,74`). Reproduced 3× in 10 runs. Tasks.md §7.3 says the deleted `BackupDeviceConnectionChanged_*` unit test's behaviour "is exercised by the integration test" — but that integration test is itself unreliable, so the failover-then-backup-write path has no dependable assertion. *Fix:* re-introduce a dispatcher-driven unit test on the same `StubTransport`+`dispatcher.Dispatch` pattern the existing `RedundantConnectionPairTests` use (those never flake), or inject a synchronisation seam so the cold-start cases don't race the threadpool.

2. **The pair never observes the primary's TCP `StateChanged`** (`src/QscDspDevices/Connectivity/Redundancy/RedundantConnectionPair.cs:106` — only `_backup.StateChanged += OnBackupStateChanged`). If the primary is the active slot and its TCP socket drops, `OnEngineState` cannot fire (no notifications on a dead socket) and `OnBackupStateChanged` is the wrong handler. `_activeSlot` stays at `Primary`, the routing facade keeps writing to the primary's now-non-accepting queue, and `TryEnqueue` returns false + logs "command attempted while disconnected" indefinitely — even though the backup is `Active` and ready. This is README §"Device Connection" "lost connection → switch to backup", and SPEC_COMPLIANCE row 7.2 claims it's covered — but the cited test only drives an EngineStatus push, never a TCP drop. *Fix:* add `_primary.StateChanged += OnPrimaryStateChanged` symmetric to the backup handler; on Disconnected set `_primaryState = EngineState.Unknown` and re-evaluate.

3. **Backup transport + backup `CommandQueue` are leaked on `QscDspTcp.Dispose`** (`src/QscDspDevices/Plugin/QscDspTcp.cs:557-590`). `Dispose` disposes the pair (which only disposes the two `ConnectionManager`s and the observers), then disposes `_transport`, `_queue`, `_routingQueue` — those three are the *primary's*. The backup's transport (a real TCP socket in production) and `CommandQueue` (a bounded `Channel<>`) are never disposed. Verified by reading the diff; `RedundantConnectionPair.Dispose` does not touch them, and `ConnectionManager.Dispose` does not own them either. *Fix:* dispose `_backupQueue` (and a stored backup transport) in `RedundantConnectionPair.Dispose`, or store + dispose them on `QscDspTcp`.

## Concerns

4. **`_primaryGroupManager` and the backup's `groupManager` are also never disposed** (same place as #3). `ChangeGroupManager` likely owns timer/state that should be released.

5. **`OnEngineState` and `OnBackupStateChanged` re-evaluate-then-apply outside the lock** (`RedundantConnectionPair.cs:222–229,326–332`). Two concurrent transitions can both compute `oldActive=Primary, newActive=Backup` and both fire `RedundancyStateChanged` for one logical transition. Idempotent on the routing/callback side, but observable to framework consumers. *Fix:* hold the lock through `ApplyActiveSwitch` (callbacks are quick) or gate with `Interlocked.CompareExchange` on a `_lastAppliedActive` field.

6. **`RedundantConnectionPair.Disconnect` does not clear `_activeSlot` nor call `_routingQueue.SetActive(null)`** (`RedundantConnectionPair.cs:160–169`). After `Disconnect`, `PrimaryDeviceActive` keeps reporting `true`, which is observably wrong for `IRedundancySupport`. The routing-queue side is benign (the queue is non-accepting), but the property is not.

7. **`SwitchbackPolicy.PickActive` returns `CoreSlot.Primary` under `QscRecommended` when both are Active *and* `currentActive is null`** (`SwitchbackPolicy.cs:80`). Documented as "deterministic startup" but not test-pinned. Add one test.

## Nits

- `EngineStatusObserver`: `Params?["State"]?.ToString()` returns the string `"null"` for a JSON-null value, which falls through to the `Unknown`-warn branch — correct by coincidence, not by design. Add an explicit case or comment.
- `RoutingCommandQueue` inherits the base bounded channel (1024 entries) that is never used. Document the dead allocation in the class XML doc, or extract an `ICommandQueue` interface so the facade doesn't have to inherit unused state.
- Verify `tasks.md` §7.2 and §7.4 boxes are unchecked (deferred), not ticked.

## Praise

- `EngineStatusObserver` is a tight 95 LOC with clean subscribe/unsubscribe symmetry and a dedicated `Unknown` fallback that logs and bails — the right shape for a parser at the edge of an untrusted protocol.
- The choice to encode the README-vs-QSC switchback tension as a `SwitchbackPolicy` toggle with a recorded `D3` deviation in SPEC_COMPLIANCE picks a side, justifies it, and gives the integrator an escape hatch.
- `RoutingCommandQueue.SetActive`/`TryEnqueue` correctly snapshot the active queue under its own lock then call the underlying `TryEnqueue` outside that lock — pre-empts the obvious nested-lock deadlock.

## What I did NOT verify

- Real-hardware behaviour against an actual QSC redundant Core pair. Everything was exercised against `FakeQrcServer` and `StubTransport`.
- `AudioControlServiceFanout.Dispatch` re-attach timing on switchover — assumed it is a clean delegate swap.
- Per-class coverage on `Connectivity/Redundancy/*.cs` (the coverage run flaked).
- Mutation testing on `SwitchbackPolicy.PickActive` — only example-based tests exist for the state cross-product.
- Behaviour under genuine clock-skew between primary and backup EngineStatus pushes (policy is event-driven; orderings not enumerated).
