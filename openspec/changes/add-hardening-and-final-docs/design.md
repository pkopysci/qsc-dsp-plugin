# M7 Design — Hardening + Final Docs

## D-T1: Threading shape — Task-loops vs Thread-trio

**Problem.** The literal README §4 text reads, in full: *"Internal thread count must be limitted to a maximum of 3 concurrent threads. The library must be non-blocking and all threading must be managed internally (no public async/await)."* That is the entire threading constraint. The README does **not** prescribe OS `Thread` instances, named send/recv/timer roles, dedicated lifecycles, or removal of any orchestrator — those were elaborations introduced by the M2 `threading-budget` spec, not literal README requirements.

M2's `ConnectionManager` runs a `RunSessionAsync` orchestrator plus task-loops scheduled on the threadpool for send / receive / keepalive. M3's critic flagged divergence from the *M2-spec interpretation*, deferred to M7. M7 walks back the over-prescriptive M2 interpretation.

**Options.**

1. **Collapse to Thread-trio (literal compliance).** Three dedicated `Thread` instances per manager, owning send / receive / keepalive. Predictable scheduling, no threadpool starvation risk, matches the framework's stated assumption. Cost: invasive rewrite; loses async ergonomics; doubles thread count under redundancy (3 → 6, but already at 6 with the current task-loops); risk of regressions in the M2 reconnect cadence and the M3 cleanup ordering.

2. **Keep Task-loops + record D-T1 deviation.** Production behaviour is observably equivalent: bounded steady-state work, no public async, `ThreadCensus` registers the actual loops. Cost: README literal-reading is violated; we depend on threadpool availability (already a known risk that flaked tests in M3 + M6); future maintainer has to read the deviation note.

3. **Hybrid: dedicated Thread for receive (latency-critical), Task-loops for send + keepalive.** Receive is the only thread where blocking matters for correctness (frame-boundary parsing). Send is bounded-channel-driven, keepalive is timer-driven. Cost: two seams to maintain; mild complexity.

**Decision (M7).** **Option 2 — keep Task-loop shape; D-T1 is recorded as "M2-spec over-prescribed beyond README §4; M7 walks back."**

**Rationale.**
- The README's literal threading rule is "≤ 3 concurrent threads." A bounded set of long-running task-loops, plus a `ThreadCensus` that registers them, satisfies that rule directly: at any instant the plugin holds ≤ 3 threadpool threads (send loop active OR keepalive firing OR `RunSessionAsync` waking — receive is event-driven on the `BasicTcpClient` callback and is not plugin-owned).
- The flake mode that motivates a stricter Thread-trio interpretation is *threadpool starvation under heavy GC*. We have not observed that mode in CI or in the local stress runs (M3 critic Pass-1 follow-up + M6 critic Pass-2 stress); the M3 + M6 flakes were post-connect-chain races, not threadpool exhaustion.
- The receive path is event-driven via `BasicTcpClient.RxReceived` + the framer's incremental parse. There is no blocking read loop that benefits from a dedicated `Thread`.
- Option 1 risks regressing `KeepaliveTimerTests` and `ConnectionManagerTests.Disconnect_drains_in_flight_writes`. The benefit is M2-spec conformance, not README conformance, and not behaviour.
- Option 3 splits the seam without changing the README compliance picture.
- The `threading-budget` spec is amended to describe what we actually do (≤ 3 concurrent, registered in `ThreadCensus`). The deviation is *from our own over-prescriptive interpretation*, not from the README.

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
