# Pass 1 — milestone/m4-routing-and-zones

**Date:** 2026-04-30 (build/test verified 2026-05-02)
**Build:** Release `-warnaserror` 0/0
**Tests:** 295/295 across 3 stress runs (274 unit + 11 integration + 10 property)
**Coverage (QscDspDevices.dll):** line 92.3% / branch 81.3% / method 95.8%; M4 classes all ≥ 91.8%
**Release DLL:** 95,232 bytes / 500 KB budget — 19% used
**Verdict:** ⚠️ ship with caveats — three real bugs in the routing service, two missing tests, no blockers.

## Blockers

(none)

## Concerns

1. **`AudioRoutingService.Clear` fires `RouteChanged` and emits a no-op `Control.Set` even when the output was never routed** (`AudioControl/AudioRoutingService.cs:135`). `UpdateCacheAndRaise(outputId, "")` treats a cache miss as a transition (`!TryGetValue` ⇒ `changed=true`), so first-time Clear on a fresh output spuriously raises the event AND writes `Value=0` over the wire. The user explicitly asked about this; no test pins either behaviour. Fix: short-circuit when the prior cached value is null/empty AND `newSourceId` is empty.

2. **`AudioRoutingService.Route` does not validate `source.BankIndex >= 1`** (`AudioControl/AudioRoutingService.cs:89-103`). `BankIndex` is a plain int with no record-level guard. A misconfigured input with `bankIndex = 0` makes Route (a) update the cache to that sourceId, (b) enqueue `Control.Set { Value: 0 }` — but `Value=0` is the QSC "no source" sentinel, so the Core *clears* the route while the cache reports it routed. No test for `bankIndex <= 0`. Fix: reject `BankIndex < 1` with `Log.Error` before enqueue/cache mutation.

3. **Cache-update-before-queue-accept-check leaks optimistic state across disconnect** (`AudioRoutingService.cs:102-103`; same pattern in `AudioZoneEnableService.cs:78` and `:111`). When the queue refuses (disconnected/disposed), the cache and event have already been mutated. `GetCurrentAudioSource` reports a source the Core was never told about. AutoPoll on reconnect reconciles, but during the disconnected window the framework reads ghost state. M3 exhibits the same shape — at minimum, document the trade-off in the routing-service XML doc; preferably, skip the cache mutation when the queue is closed.

4. **Tag-collision silent overwrite in `AudioChannelRegistry._tagToChannelId`** (`AudioChannelRegistry.cs:261-272`). Two channels declaring the same `LevelTag`, or a `RouterTag` that collides with another channel's `LevelTag`/`MuteTag`, get last-writer-wins on the reverse map. The fanout's `IsRouterTag`-first precedence hides router-vs-zone collisions but not channel-vs-channel. No log, no detection. Add a `Log.Warn` on duplicate tag insertion, symmetric to the existing duplicate-channel `Log.Notice`.

5. **`HydrateChangeGroupAction` silently absorbs per-control enqueue failures** (`Connectivity/PostConnect/HydrateChangeGroupAction.cs:130-162`). Each `_queue.TryEnqueue` returning `false` is dropped; only the all-zero case warns. A partial subscribe leaves AutoPoll cycling stale absences. Pre-existing in M3, expanded surface in M4. Fix: `Log.Warn` per failed enqueue with the tag name. (Group cap is fine — verified single group, leaf controls.)

## Nits

- `SPEC_COMPLIANCE.md:59` says "10 cases" for `AudioRoutingServiceTests`; file has 12 `[Fact]` methods.
- `AudioControlServiceFanout` `<remarks>` documents precedence but not rationale; the proposal-level reasoning ("routerTag is hardware-singular; zone-enables are by-pair so router wins") belongs in the code comment.
- `AudioRoutingService.OnDeviceUpdate` early-out (`:162-170`) is stale-tag-safe via the M3 `ClearAutoPolls` (verified in `ConnectionManager.cs:628`); the redundant equality check on `output.RouterTag == delta.Name` is correct belt-and-braces but under-commented. One line on why both `TryGetChannelIdByTag` and the equality check are needed (handles re-registration mid-flight) would help.
- `AudioZoneRegistry.TryRegister` returning `false` is correctly absorbed by `QscDspTcp.AddAudioZoneEnable` — no exception, no event surfaced. Verified.

## Praise

- `IsRouterTag` / `IsZoneTag` predicates make `AudioControlServiceFanout.Dispatch` O(1) without scanning the channel map — clean separation between dispatch (allocation-free) and resolution (in the consumer).
- `AudioChannelRegistry.Register` correctly removes the *prior* router-tag and bank-index entries on re-registration (`:251-257`). Right shape for config-refresh-mid-session and the explanatory comment is load-bearing. No regression of the M3 stale-AutoPoll fix.
- Integration env wires `groupManager.SetDeltaCallback(fanout.Dispatch)` in production shape; tests pin both `Control.Set` round-trip and `AudioRouteChanged` from a server push.

## What I did NOT verify

- Mutation testing on the routing/zone services.
- Behaviour on real QSC hardware.
- AutoPoll → cache → event invariant under heavy concurrent producer load (only single-threaded delta dispatch tested).
- 15 s reconnect cadence under a routing-mid-flight disconnect (no integration test for Route → disconnect → reconnect → rehydrate).
- Whether the redacting log formatter scrubs zone controlTags that might embed credentials (none of the M4 surfaces accept passwords; tag names are echoed verbatim in `Re-registering` notices).
