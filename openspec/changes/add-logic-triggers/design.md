# Design — Add DSP logic trigger support

## Context

The smallest of the four optional `IDsp` side-interfaces, two methods
plus one event. The code shape mirrors the M3 mute control + M4 zone
enable patterns: registry → service → fanout dispatch → hydration
subscribes the registered tags. The only structural addition is a
fourth branch in `AudioControlServiceFanout`.

## Goals / Non-Goals

**Goals**
- `IDspLogicTriggerSupport` (2 methods + 1 event) end-to-end against
  the FakeQrcServer.
- Hydration extended to subscribe every registered trigger tag.
- Cache-less by design — see scope decision §2 in the proposal.

**Non-Goals**
- Stretched-pulse behaviour, configurable Value-on-pulse,
  Designer-side reset emulation.
- Any use of `Component.Set` (the M3+M4 services only use
  `Control.Set`; triggers are also leaf controls and don't need
  Component-level addressing).

## Architecture

```
QscDspTcp
   │
   ├── AddDspLogicTrigger / PulseDspLogicTrigger
   │       │
   │       ▼
   │   LogicTriggerService (uses LogicTriggerRegistry)
   │       │ (Control.Set { Name=tagName, Value=true })
   │       ▼
   │   CommandQueue → JsonRpcDispatcher
   │       ▲                    │
   │       │                    │ (AutoPoll deltas on triggerTag)
   │       │                    ▼
   │       │         AudioControlServiceFanout (4-way)
   │       │                    │
   │       └──────── LogicTriggerService.OnDeviceUpdate
   │                            │ (no cache; raise event on every delta)
   │                            ▼
   │                   QscDspTcp.DspLogicTriggerStateChanged
```

Fanout precedence becomes: routerTag → zoneTag → triggerTag →
audio control. The added `IsTriggerTag` predicate is O(1).

## Key design decisions

### D-1: Cache-less event raise

The framework's `DspLogicTriggerStateChanged` is a Single-arg event
("this trigger transitioned"), not Dual ("trigger X is now Y"). The
documentation literal "Triggered whenever a monitored trigger
control changes" supports two readings:

- **Coalesced**: only fire on transitions; cache the last value.
- **Per-delta**: fire on every AutoPoll delta for a registered tag.

We pick per-delta. Coalescing assumes the trigger has a stable state
between fires; QSC's momentary-trigger pattern explicitly does not
hold state — a fast pulse-pulse-pulse sequence on a momentary
trigger needs the framework to see three events, not one. Per-delta
also lets the framework decide what to coalesce; the alternative
direction (we coalesce, framework wants more granular) cannot be
recovered.

### D-2: Pulse = single Value=true write

QSC trigger controls in Designer typically auto-reset on the design
side. Writing `false` after `true` would either be a no-op (the
design already reset) or worse, race the reset and fire a second
edge transition. Sending only `true` is the canonical QRC pulse
pattern (see `research/QRC_PROTOCOL.md` §4 — Named Controls).

### D-3: Replace-on-duplicate `Add`

`IDspLogicTriggerSupport.AddDspLogicTrigger` doesn't specify
duplicate-id behaviour. The M3 audio registry replaces and logs
Notice; the M4 zone registry drops and logs Notice (per its
explicit framework spec). We mirror M3 for triggers because (a) the
framework spec doesn't mandate drop, and (b) replace is the more
useful behaviour during Designer config refreshes (a re-register
typically means "the tagName moved").

### D-4: Fanout precedence

Order: router → zone → trigger → audio. The first three are
"more specific" features; audio is the catch-all. Tags
hypothetically claimed by two of the M3/M4/M5 features are
Designer-side configuration errors; we pick a deterministic order
and pin it. Trigger placement before audio means a tag that's
both a trigger AND an audio level/mute fires the trigger event,
not the audio event — almost certainly the user's intent.

## Risks

1. **AutoPoll burst on a fast-pulsing trigger.** A trigger that
   pulses 10× per second pushes 10 AutoPoll deltas per 250 ms
   window into the receive thread. The receive thread synchronously
   raises the framework event for each. A slow framework subscriber
   could back-press. Same risk surface as M3+M4; M3's documented
   mitigation (drop log on overshoot, don't drop frames) carries
   over.
2. **Trigger tag colliding with an audio level tag.** Surfaced as a
   Designer error via the M4 `WarnIfTagCollides` shape (extended
   to also detect trigger collisions). The fanout precedence
   ensures the trigger event fires; the audio cache for the
   shadowed channel grows stale until the next non-colliding
   delta arrives.
