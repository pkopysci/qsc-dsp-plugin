# Change: Add audio control (level/mute) and snapshot presets

## Why

M1 delivered the build foundation; M2 delivered the QRC client plumbing
(framer, dispatcher, command queue, connection manager, transport,
ThreadCensus, FakeQrcServer). Neither milestone produced any end-user-
observable behaviour: a freshly-connected `QscDspTcp` plugin returns
empty channel/preset id lists and every `Set/Get` audio call is a
documented `NotSupported` no-op.

This milestone delivers the first slice of real DSP control: the full
`IAudioControl` surface (level, mute, preset recall, plus the four
audio-state-change events) along with the change-group machinery that
keeps the plugin's cached state in step with the Core. It is the
milestone that turns the plugin from "talks to a Core" into "controls
audio on a Core" — every higher-level milestone (routing in M4, logic
triggers in M5, redundancy in M6) builds on top.

The key protocol-level changes versus M2:

- **Active QRC commands.** `Control.Set` / `Control.Get` (and where
  necessary `Component.Set` / `Component.GetControls`) for level and
  mute against the level-tag and mute-tag strings supplied to
  `AddInputChannel` / `AddOutputChannel`. `Snapshot.Load` for preset
  recall, addressed by `bank` (Snapshot Bank component code-name) and
  `index` (1..N) supplied to `AddPreset` per `research/QRC_PROTOCOL.md`
  §6.1.
- **Change-group subscriptions.** A single change group per connection
  containing every registered level/mute control, configured for
  AutoPoll. Server-pushed AutoPoll responses drive the `IAudioControl`
  events (`AudioInputLevelChanged`, `AudioInputMuteChanged`,
  `AudioOutputLevelChanged`, `AudioOutputMuteChanged`) without the
  plugin polling. The QRC protocol caps change groups at 4 per
  connection — M3 uses one; M4–M6 may add more.
- **Logon post-connect action.** When `Initialize` was called with a
  non-empty username/password, the connection manager issues a
  `Logon` JSON-RPC as the first request after every (re)connect,
  before subscribing the change group or sending any other command.
  Credentials are redacted from the framer's debug log path (the
  pass-2 critic flagged this as M2 deferred work).
- **3-thread runtime budget — actual.** M2 ran the entire session as
  one threadpool task; the README's 3-thread budget existed but
  wasn't really used. M3 introduces three dedicated `Thread`
  instances — a send loop draining the `CommandQueue`, a receive
  loop dispatching JSON-RPC framing/responses/notifications, and a
  shared timer for keepalive + reconnect — each registering with
  the existing `ThreadCensus`. The single task-based session loop
  is removed; `ConnectionManager.RunSessionAsync` becomes a thin
  orchestrator that starts the trio on Connect and joins them on
  Disconnect.

This milestone does **not** deliver:

- Matrix routing or `IAudioRoutable` (M4).
- Audio zone gating or `IAudioZoneEnabler` (M4).
- Logic-trigger support or `IDspLogicTriggerSupport` (M5).
- Redundancy / failover or `IRedundancySupport` (M6).
- ECP protocol support (separate milestone).
- Multi-Core support (M6).

## What Changes

- **New capability spec `audio-control`.** Defines the QRC mapping for
  every method on `IAudioControl`, the 0–100 framework ↔ device-native
  level scaling using `levelMin`/`levelMax` from `AddInputChannel` /
  `AddOutputChannel`, and the lookup semantics for unregistered ids
  (`Get*` returns 0 / false; `Set*` logs error and is a no-op, per the
  framework spec).
- **New capability spec `change-group-subscriptions`.** Defines the
  single change-group lifecycle: created during the post-connect
  hydration sequence after Logon (if any), populated with every
  level-tag and mute-tag registered via `Add*Channel`, configured for
  AutoPoll at 250 ms, torn down on Disconnect, recreated on every
  reconnect.
- **Modified capability spec `connection-manager`.** Adds the Logon
  post-connect action (when credentials are configured), the
  change-group hydration step, and the change-group teardown on
  Disconnect. Removes M2's "post-connect action list is empty"
  guarantee.
- **Modified capability spec `threading-budget`.** Replaces the
  M2 wording ("M2 captures one threadpool task; M3 will introduce
  dedicated send/receive/timer threads") with the actual 3-thread
  layout, including the lock-order and ownership rules for each
  thread.

- **Source code (under `src/QscDspDevices/`):**
  - `AudioControl/AudioChannelRegistry.cs` — thread-safe channel and
    preset registry. Records `(id, levelTag, muteTag, levelMin,
    levelMax, isInput)` per channel and `(id, bank, index)` per
    preset.
  - `AudioControl/LevelScaler.cs` — pure utility: `ToDevice(int
    framework, int min, int max)` and `ToFramework(int device, int
    min, int max)` with explicit half-up rounding and clamp at the
    edges.
  - `AudioControl/AudioControlService.cs` — orchestrates `Control.Set`
    /`Control.Get`/`Snapshot.Load` calls against the
    `JsonRpcDispatcher`, holds the cached current values, and raises
    the four `IAudioControl` events when a cached value changes.
  - `AudioControl/ChangeGroupManager.cs` — owns the single AutoPoll
    change group, builds subscribe / unsubscribe payloads per
    `research/QRC_PROTOCOL.md` §5, parses AutoPoll deltas into level
    / mute updates feeding `AudioControlService`.
  - `Connectivity/PostConnectActions/LogonAction.cs` — implements the
    `IPostConnectAction` from M2 for the Logon step. Idempotent:
    skips when no credentials are configured.
  - `Connectivity/PostConnectActions/HydrateChangeGroupAction.cs` —
    implements `IPostConnectAction` for the change-group bootstrap.
  - `Plugin/Threading/PluginThreads.cs` — owns the three `Thread`
    instances (send, receive, timer), each registering with
    `ThreadCensus`, started in `ConnectionManager.OnConnected` and
    joined in `OnDisconnecting`.
  - `Plugin/QscDspTcp.cs` — extends the M2 stub: implements the 12
    `IAudioControl` methods + the 4 events; raises events on the
    framework's expected thread per README §3.
  - `Protocol/Logging/RedactingDebugFormatter.cs` — debug-log
    formatter that scrubs password fields out of `Logon` payloads
    (and only those payloads) before they hit `Logger.Debug`.

- **Tests:**
  - Unit (xUnit + Moq): `AudioChannelRegistryTests`, `LevelScalerTests`
    (FsCheck property: round-trip preserves to within ±1 framework
    level over the full range), `AudioControlServiceTests`,
    `ChangeGroupManagerTests`, `LogonActionTests`,
    `RedactingDebugFormatterTests`, `PluginThreadsTests`.
  - Integration (xUnit + FakeQrcServer): connect + Logon happy path;
    `Set/GetAudioInputLevel` round-trip with scaling; mute state
    pushed via AutoPoll fires `AudioInputMuteChanged`; preset recall;
    reconnect re-subscribes the change group; failed Logon
    classification.
  - Property: `LevelScaler` round-trip; `ChangeGroupManager` parse of
    arbitrary AutoPoll JSON.

- **Documentation:**
  - Update `ARCHITECTURE.md` with the actual 3-thread layout, the
    post-connect action list, and the change-group lifecycle.
  - Update `SPEC_COMPLIANCE.md`: discharge the README rows for level
    control, mute control, preset recall, the four audio events, the
    3-thread budget, and Logon credential handling.

## Scope-edge decisions (called out so the critic doesn't have to hunt)

1. **One change group, AutoPoll at 250 ms.** The QRC protocol caps
   change groups at 4 per connection. Splitting per-feature (one for
   audio, one for routing, one for triggers, one for redundancy) hits
   that ceiling exactly and leaves no headroom. We use one combined
   group from M3 onwards. AutoPoll at 250 ms balances server load
   against UI responsiveness; QSC's reference docs use 200–500 ms for
   "state-snapshot" controls. 250 ms is in the middle and a clean
   `int` value.
2. **Get* methods serve from the cache, not the wire.** `IAudioControl`
   does not specify whether `Get` blocks on a round-trip. Returning
   from cache gives the framework O(1) reads (no thread blocking, no
   queue back-pressure). The cache is initially zero; the first
   AutoPoll response after subscribe populates it. Tests must
   explicitly handle the "before first AutoPoll" window; we do not
   expose a "ready" signal in this milestone.
3. **Unknown-id semantics match the framework doc literally.** `Get`
   returns `0` / `false` per `framework-docs/gcu-hardware-service/IAudioControl.md`.
   `Set` logs `Logger.Error` and returns silently (the framework
   surface is `void`).
4. **Logon credentials live in `QscDspTcp` only.** `Initialize` stores
   them; `LogonAction` reads them from a callback. They never touch
   the framer except via the redacting formatter. Rotating credentials
   requires a `Disconnect → Initialize → Connect` cycle.
5. **Snapshot ramp is fixed at 0 in M3.** The `IAudioControl.RecallAudioPreset`
   surface has no ramp parameter. M3 sends `Snapshot.Load` without
   `Ramp` (Core defaults to 0). A configurable ramp is a future
   capability; not in scope here.
