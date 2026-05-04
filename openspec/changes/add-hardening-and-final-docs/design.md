# M7 Design — Hardening + Final Docs

## D-T1: Threading shape — Task-loops vs Thread-trio

**Problem.** README §4 prescribes "no more than 3 internal threads (per-manager): one for send, one for receive, one for keepalive/timer." M2's `ConnectionManager` instead runs a single `RunSessionAsync` task that drives a state machine and dispatches send/receive/keepalive on the threadpool. M3's critic deferred the divergence to M7 (M3-tasks §4.5 + §4.6).

**Options.**

1. **Collapse to Thread-trio (literal compliance).** Three dedicated `Thread` instances per manager, owning send / receive / keepalive. Predictable scheduling, no threadpool starvation risk, matches the framework's stated assumption. Cost: invasive rewrite; loses async ergonomics; doubles thread count under redundancy (3 → 6, but already at 6 with the current task-loops); risk of regressions in the M2 reconnect cadence and the M3 cleanup ordering.

2. **Keep Task-loops + record D-T1 deviation.** Production behaviour is observably equivalent: bounded steady-state work, no public async, `ThreadCensus` registers the actual loops. Cost: README literal-reading is violated; we depend on threadpool availability (already a known risk that flaked tests in M3 + M6); future maintainer has to read the deviation note.

3. **Hybrid: dedicated Thread for receive (latency-critical), Task-loops for send + keepalive.** Receive is the only thread where blocking matters for correctness (frame-boundary parsing). Send is bounded-channel-driven, keepalive is timer-driven. Cost: two seams to maintain; mild complexity.

**Decision (M7).** **Option 2 — keep the Task-loop shape, record as deviation D-T1.**

**Rationale.**
- The flake mode that motivates the README rule is *threadpool starvation under heavy GC*. We have not observed that mode in CI or in the local stress runs (see M3 critic Pass 1 follow-up); the M3 + M6 flakes were post-connect-chain races, not threadpool exhaustion.
- The receive path is event-driven via `BasicTcpClient.RxReceived` + the framer's incremental parse — there is no blocking read loop that benefits from a dedicated `Thread`.
- Option 1 risks regressing the M2 keepalive cadence test (`KeepaliveTimerTests`) and the M3 cleanup ordering test (`ConnectionManagerTests.Disconnect_drains_in_flight_writes`). The benefit is documentation conformance, not behaviour.
- Option 3 splits the seam without removing the deviation; we still have to record D-T1 for the send/keepalive halves.
- The threading-budget spec is amended to describe what we actually do; `ThreadCensus` is wired to register the steady-state loops so the runtime guard reflects the truth.

**Reversibility.** One-way door. Future versions could re-introduce a dedicated `Thread` for receive without breaking semver as long as `ThreadCensus` and `IRedundancySupport` semantics stay unchanged.

## D-T2: Mid-session AddInputChannel / AddOutputChannel / AddPreset

**Problem.** M3-tasks §6.3 deferred the mid-session subscribe path. Today the registry accepts the call at any time, but `ChangeGroupManager.AddControl(name)` is only invoked during the post-connect hydration. Mid-session adds silently never get polled.

**Decision (M7).** Wire the mid-session add: registry call with an active session triggers a single `ChangeGroup.AddComponentControl` write + a one-shot `ChangeGroup.Poll` to seed the cache. If the session is `Disconnected`, the registry add is staged and applied during the next post-connect hydration (existing behaviour).

**Tradeoff.** The hydration code path is now reached from two seams (initial Logon→Hydrate, and live `AddX` calls). Both seams call the same `ChangeGroupManager.AddControl(name)`; the live path skips the wholesale `Hydrate` rebuild and only adds the new control. Verified by `Mid_session_add_subscribes_new_control` integration test.

## D-T3: ChangeGroup.Destroy on Disconnecting

**Problem.** M3-tasks §2.3 deferred the courtesy `ChangeGroup.Destroy` write. The Core GCs the group on socket close anyway, so it is not a correctness issue — but a long-lived integrator that connects/disconnects in a loop will leave a trail of orphan groups on the Core until the socket closes.

**Decision (M7).** Issue a single `ChangeGroup.Destroy` on `ConnectionState.Disconnecting`, gated by `_transport.IsConnected == true`. If the write fails (transport already torn down, queue refused, exception), log `Logger.Warn` and continue with the disconnect — the Core's socket-close GC is the safety net.

**Reversibility.** Reversible — the Destroy is best-effort and leaves no public-surface footprint.

## D-T4: Public-API surface freeze

**Problem.** The next milestone (ECP) and any future framework-version bump need a stable QRC surface to layer on. Today the public surface is whatever happens to be `public` for test access.

**Decision (M7).** Add `Microsoft.CodeAnalysis.PublicApiAnalyzers` (or `PublicApiGenerator` snapshot tests — pick whichever has lighter analyzer overhead). Generate `PublicAPI.Shipped.txt` at the end of M7. Any subsequent change must either edit `PublicAPI.Unshipped.txt` (additive) or open an explicit "API change" proposal.

Any symbol that is `public` only because tests touch it gets `[EditorBrowsable(EditorBrowsableState.Never)]` + an `internal`-but-`InternalsVisibleTo` migration when feasible. Symbols documented as test-only in their XML doc are exempt from this audit and stay `public` (e.g., `RedundantConnectionPair.Primary`/`Backup`).

## D-T5: Coverage gate 90 % → 92 %

Current local merged coverage is 91.1 %; CI is platform-variable down to 89.6 %. M7 closes both gaps:

- M3-tasks §11.4 wraps up: the M3 `QscDspTcp` paths still uncovered (the redundant ctor branch, the `BuildRedundantPair` no-backup early-return, the `Dispose` double-dispose guard) get tests.
- The two integration tests in §1 (M6-7.4 + M6-7.2) lift the `RedundantConnectionPair` file to ≥ 95 %.

Once local hits ≥ 93 %, the CI 92 % gate is comfortable.

## What we explicitly will NOT do in M7

- ECP. Separate milestone.
- Asynchronous public surface. README §4.2 forbids it; not revisited.
- Replacing `BasicTcpClient` with `System.Net.Sockets`. Framework binding constraint.
- Adding new control types (matrix-of-matrices, snapshot save/restore, etc.). Not in any framework interface we're bound to.
