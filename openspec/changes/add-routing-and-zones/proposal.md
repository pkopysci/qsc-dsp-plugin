# Change: Add matrix routing and audio zone enables

## Why

M3 turned the plugin into something useful for the level / mute /
preset slice of `IAudioControl`. The two remaining DSP surface areas
the framework expects from any "DSP plugin worth using" — matrix
routing (`IAudioRoutable`) and per-zone enable matrices
(`IAudioZoneEnabler`) — land in M4. Together these make the plugin
capable of running the full source-selection-plus-output-zoning
pattern that QSC Cores typically host as a Matrix Mixer plus an array
of zone-enable booleans on each input strip.

The protocol-level shape is straightforward — both features ride on
the same Q-SYS Named Controls / Component Controls infrastructure M3
already wired (`Control.Set` / AutoPoll deltas via the
`qsc-plugin-state` change group). What changes are the per-feature
caches, the event surfaces, and the registry shapes.

The key new pieces:

- **Audio routing.** Each registered output channel carries a
  `routerTag` (M3 captures this in `AudioChannelRegistry` but
  ignores it). M4 treats `routerTag` as a Q-SYS Named Control
  pointing at a "selected source" control on the matrix mixer
  component. `RouteAudio(sourceId, outputId)` resolves the
  source's bank-index from `AudioChannelRegistry` and issues
  `Control.Set { Name = routerTag, Value = bankIndex }`.
  `ClearAudioRoute(outputId)` sends `Value = 0`.
  `GetCurrentAudioSource(outputId)` serves from a per-output cache
  populated by AutoPoll deltas on the routerTag.
- **Audio zone enables.** A separate `AudioZoneRegistry` holds
  `(channelId, zoneId) → controlTag` triples registered via
  `AddAudioZoneEnable`. The control is a boolean named control on
  the Q-SYS design. `Set`/`Toggle`/`Query` map to `Control.Set`/
  cached-read just like mute control from M3. `Toggle` reads the
  cached value, flips, and sends.
- **Change-group expansion.** The hydration action's subscription
  list grows: in addition to every level-tag and mute-tag, M4 now
  subscribes every output channel's `routerTag` (when non-empty)
  and every registered zone-enable `controlTag`. The single
  `qsc-plugin-state` group still fits all of these well under the
  QRC 4-group cap — every M4 control is a leaf control, not a
  separate group.

This milestone does **not** deliver:

- Logic-trigger support (`IDspLogicTriggerSupport`) — M5.
- Redundancy / failover (`IRedundancySupport`) — M6.
- ECP protocol support — separate milestone.
- Multiple matrix mixers per Core — the registry assumes one
  `routerTag` per output, which matches the framework's
  one-routerTag-per-AddOutputChannel surface.
- Mid-session `AddAudioZoneEnable` dynamic subscribe — registry
  registration works at any time, but the change-group subscribe
  path is post-connect-hydration only (same shape as M3 task 6.3).

## What Changes

- **New capability spec `audio-routing`.** Defines the QRC mapping
  for `IAudioRoutable.RouteAudio` / `ClearAudioRoute` /
  `GetCurrentAudioSource`, the per-output cache semantics, and the
  AutoPoll-driven `AudioRouteChanged` event raise contract.
- **New capability spec `audio-zones`.** Defines the
  `(channelId, zoneId)` registry shape, the
  `Set`/`Toggle`/`Query`/`Add`/`Remove` mappings, and the
  `AudioZoneEnableChanged` event raise contract. `Toggle` is
  documented as a cached-read-then-flip-and-send so the framework's
  toggle UX matches the displayed cached state.
- **Modified capability spec `change-group-subscriptions`.** Hydration
  now subscribes routerTags and zone-controlTags in addition to the
  M3 level/mute tags. The 4-group cap is unaffected (M4 still uses
  exactly one group, `qsc-plugin-state`).

- **Source code (under `src/QscDspDevices/`):**
  - `AudioControl/AudioRoutingService.cs` — orchestrates `Control.Set`
    against the routerTag, holds the per-output `currentSourceId`
    cache, raises `AudioRouteChanged` on cache transitions.
  - `AudioControl/AudioZoneRegistry.cs` — thread-safe map
    `(channelId, zoneId) → controlTag`. Keyed on the pair, not just
    the channelId, because one channel can be in many zones.
    `Add`/`Remove`/`TryGet`/`GetAll`. Tag→key reverse map for
    AutoPoll dispatch.
  - `AudioControl/AudioZoneEnableService.cs` — `Set`/`Toggle`/`Query`
    against the controlTag; cache + `AudioZoneEnableChanged` event
    raise; structurally a sibling of the M3 mute-control path.
  - `Plugin/QscDspTcp.cs` — implements the `IAudioRoutable` (3
    methods + 1 event) and `IAudioZoneEnabler` (5 methods + 1 event)
    surfaces; delegates to the two new services. The `BackupDevice*`
    properties on `IRedundancySupport` are still M6 — untouched.

- **Tests:**
  - Unit (xUnit + Moq + FsCheck): `AudioZoneRegistryTests`,
    `AudioRoutingServiceTests`, `AudioZoneEnableServiceTests`. The
    routing service tests cover the `Control.Set` wire shape on
    `RouteAudio`, the zero-on-clear behaviour, the cached-read on
    `GetCurrentAudioSource`, and the AutoPoll-delta-driven
    `AudioRouteChanged` event raise.
  - Integration (xUnit + FakeQrcServer): one happy-path round-trip
    for routing (`RouteAudio` produces the expected `Control.Set`
    on the wire), one for zone enable (`SetAudioZoneEnable` ditto),
    and one server-pushed AutoPoll delta on a routerTag firing
    `AudioRouteChanged`.

- **Documentation:**
  - Update `ARCHITECTURE.md`'s post-connect hydration section to
    note that M4 expands the subscribe list (routerTags +
    zone-controlTags).
  - Update `SPEC_COMPLIANCE.md`: discharge the README rows for
    matrix routing (5.4, 6.4) and the four routing/zone events.

## Scope-edge decisions (called out so the critic doesn't have to hunt)

1. **Source identity is the channel-id, not the bank-index.** The
   framework calls `RouteAudio(sourceId, outputId)` with the
   framework-side IDs from `AddInputChannel`. We resolve the
   source's `bankIndex` (registered in M3 alongside everything
   else) and send that as the `Value`. Returning the framework
   sourceId from `GetCurrentAudioSource` therefore requires a
   reverse map `bankIndex → channelId`, which the registry maintains.
2. **Clear = source 0, not a separate command.** QSC matrices use a
   sentinel value (typically `0`) on the source-select control to
   mean "no source". The framework's `ClearAudioRoute` maps to
   `Control.Set { Name = routerTag, Value = 0 }`. If a particular
   design uses a different sentinel, that's a Designer-side
   configuration concern — the plugin treats `0` as the "cleared"
   value uniformly.
3. **`Toggle` is read-cached-then-set, not a separate QRC method.**
   QRC has no `Control.Toggle`. `ToggleAudioZoneEnable` reads the
   cached boolean, sends `Control.Set` with the inverted value, and
   updates the cache optimistically. This matches M3's mute path
   semantically.
4. **Zone enable controls subscribe per `(channelId, zoneId)` pair.**
   Each pair is one row in the registry and one entry in the
   change-group subscription list. A channel in N zones therefore
   contributes N subscriptions; `IAudioZoneEnabler` has no
   "all-zones-for-channel" surface, so this is the natural shape.
5. **`AddAudioZoneEnable` ignores duplicates per the framework spec.**
   The `IAudioZoneEnabler.AddAudioZoneEnable` doc explicitly says
   "If a control object with matching `channelId` and `zoneId` is
   detected then the new one will be ignored." We honour that
   verbatim — no `Logger.Notice` even, since a duplicate is the
   documented expected outcome on a config refresh.
