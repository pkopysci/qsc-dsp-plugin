# Design — Add matrix routing and audio zone enables

## Context

M3 delivered the level / mute / preset slice of `IAudioControl` plus
the active QRC plumbing (Logon, change-group AutoPoll subscribe,
send/receive/keepalive Tasks). M4 fills in the remaining DSP control
surfaces: matrix routing via `IAudioRoutable` and per-zone enables via
`IAudioZoneEnabler`.

Both feature areas ride on the same QRC infrastructure M3 already
wired:

- One change group (`qsc-plugin-state`) at 250 ms AutoPoll. M4 grows
  the subscribed-control list; the cap-at-4-groups budget is
  unaffected.
- `Control.Set` for outbound writes. The cache is updated optimistically
  on Set and reconciled by the next AutoPoll on the relevant tag.
- `AudioControlService` is the model — the M4 services are siblings,
  not subclasses.

## Goals / Non-Goals

**Goals**

- `IAudioRoutable` (3 methods + 1 event) implemented end-to-end
  against `FakeQrcServer`.
- `IAudioZoneEnabler` (5 methods + 1 event) implemented end-to-end.
- M3 hydration extended to subscribe routerTags + zone controlTags.
- M3 cache pattern (Set updates cache before/regardless of queue
  accept; AutoPoll reconciles) extended to routing + zones.

**Non-Goals**

- Logic triggers (M5), redundancy (M6), ECP (separate milestone).
- Multiple matrix mixers per Core. The framework registers one
  `routerTag` per `AddOutputChannel`; we honour that.
- "All zones for a channel" bulk operations. `IAudioZoneEnabler` is
  pair-keyed and the implementation matches.
- A separate change group for routing or zones. One group still fits
  every level/mute/router/zone control well under the 4-group cap;
  splitting would burn cap room without buying anything.

## Architecture

```
              QscDspTcp (public)            <-- IAudioRoutable + IAudioZoneEnabler entry points
                  │
                  ├─ RouteAudio / ClearAudioRoute / GetCurrentAudioSource
                  │       │
                  │       ▼
                  │   AudioRoutingService
                  │       │
                  │       │ (Control.Set on routerTag)
                  │       ▼
                  │   CommandQueue → JsonRpcDispatcher
                  │       ▲                    │
                  │       │                    │
                  │       └── (AutoPoll deltas on routerTag)
                  │           │
                  │           ▼
                  │       AudioRoutingService.OnDeviceUpdate
                  │           │ updates output→source cache
                  │           │ raises AudioRouteChanged
                  │
                  └─ Set/Toggle/Query/Add/RemoveAudioZoneEnable
                          │
                          ▼
                      AudioZoneEnableService (uses AudioZoneRegistry)
                          │
                          │ (Control.Set on the (channelId, zoneId) controlTag)
                          ▼
                      same CommandQueue → dispatcher path
```

Both new services hook into the existing
`ChangeGroupManager.SetDeltaCallback` chain by installing an
additional dispatch step. The chain becomes:

```
AutoPoll delta → ChangeGroupManager.HandleAutoPollPush → callback
                                                            │
                                                            ▼
                                              AudioControlServiceFanout.Dispatch
                                                            │
                                              ┌─────────────┼─────────────┐
                                              ▼             ▼             ▼
                                      AudioControlService  AudioRoutingService  AudioZoneEnableService
                                       (level/mute)         (router)            (zone enable)
```

The fan-out chooser uses the registry reverse-lookups: each registry
exposes a "is this tag mine?" predicate. The fan-out is
allocation-free (no list-of-callbacks) — three explicit if-else
branches in declaration order.

## Key design decisions

### D-1: Source identity translation

`RouteAudio(sourceId, outputId)` takes framework IDs. The Q-SYS Core
uses bank indices on its matrix-mixer source-select control. We
resolve the source's `bankIndex` from `AudioChannelRegistry` (M3
already records `bankIndex` on every input channel) and send that as
the `Value` of the routerTag's `Control.Set`.

`GetCurrentAudioSource(outputId)` is the inverse: the cache holds
the integer source value last seen on the routerTag's AutoPoll
delta; we resolve that bank-index back to a framework `channelId`
through a `bankIndex → channelId` reverse map maintained by the
registry.

The reverse map is kept in `AudioChannelRegistry` rather than in the
routing service so the same translation is available when M5 / M6
need it (e.g. logic triggers may also reference inputs by bankIndex).

### D-2: Clear = source value 0

QSC matrices treat `0` as "no source selected" by convention.
`ClearAudioRoute(outputId)` sends `Control.Set { Name = routerTag,
Value = 0 }`. The cached `currentSourceId` is set to the empty
string (the framework's documented "no source" marker per
`IAudioRoutable.GetCurrentAudioSource` returns spec).

If a particular design uses a non-zero sentinel (some custom
matrices reserve `-1` or a high-numbered "muted" position), that is
a Designer-side configuration choice; the plugin uniformly emits
`0`. The QSC research documents `0` as the canonical sentinel.

### D-3: Toggle is read-cached-flip-set

QRC has no `Control.Toggle` method. `ToggleAudioZoneEnable(channelId,
zoneId)` reads the cached boolean for that pair, sends
`Control.Set { Value = !cached }`, and updates the cache to
`!cached`. The framework UI sees the toggle take effect immediately
(optimistic update); the next AutoPoll on that controlTag reconciles
in the unlikely case the Core rejected the write.

A pair that has never been seen by an AutoPoll has cache = `false`;
the first toggle therefore sends `true`. This matches the framework
default (zone disabled) and avoids the ambiguity of a "I don't know
the current state" error path that the void-returning surface can't
expose.

### D-4: Per-pair subscription, not per-channel

The change-group subscribe list holds one entry per
`(channelId, zoneId)` pair, because each pair has a distinct
controlTag. A channel in N zones contributes N subscriptions. With
typical zone counts (4–16) and channel counts (8–32), the total
subscribe list grows to a few hundred entries — well within the
QRC protocol's per-control limits and the AutoPoll bandwidth budget
at 250 ms.

If a future stress reveals AutoPoll burst overload at large N, the
existing receive-thread time-budget mitigation from M3 (drop log on
overshoot, don't drop frames) handles it gracefully without
changing this architecture.

### D-5: AutoPoll fan-out via registry reverse-lookup

`AudioControlServiceFanout.Dispatch(delta)`:

```
if (channelRegistry.IsRouterTag(delta.Name)) routingService.OnDeviceUpdate(delta);
else if (zoneRegistry.IsZoneTag(delta.Name)) zoneService.OnDeviceUpdate(delta);
else audioControlService.OnDeviceUpdate(delta);
```

`IsRouterTag` and `IsZoneTag` are O(1) hash-lookups on per-registry
reverse maps. Audio-control service runs last because it owns the
"unknown tag" fast path (already silent); a level/mute tag that
also matches a router or zone tag would be an upstream config error
caught by registry validation.

## Risks

1. **Bank-index → channel-id map can desync if `AddInputChannel`
   re-registers the same id with a new bankIndex.** The M3 registry
   handles tag-remapping on re-registration; M4 needs the same for
   the bank-index reverse map. The fix is symmetric — remove the
   stale entry on re-registration and add the new one. Tests must
   cover this explicitly.
2. **`Toggle` race against AutoPoll.** If the cached value is stale
   (an out-of-band controller flipped the zone moments ago) the
   toggle sends a `Control.Set` to a now-incorrect inverted value.
   The next AutoPoll reconciles, but the framework UI shows a
   transient wrong-state. Acceptable per spec — the framework's
   toggle UX is "ask for the opposite of what you see"; what you
   see is the cache.
3. **Silent ignore of duplicate `AddAudioZoneEnable`.** Per the
   framework doc literal, duplicates are dropped without a log.
   This means a Designer-side typo (two distinct controlTags
   registered against the same pair) silently picks the first; the
   second never reaches the wire. We accept the literal spec but
   add a Notice-level log noting the drop, breaking the spec's
   "no action" wording in the most useful direction.
