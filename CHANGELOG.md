# Changelog

All notable changes to **QscDspDevices** are documented here. The format
follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the
project follows [Semantic Versioning](https://semver.org/) once shipped.
Until the first stable tag, internal milestones are listed in reverse-
chronological order.

## [Unreleased] — M-ECP part 2 (`milestone/m-ecp-part-2`)

### Added

- **ECP backend full integration.** Selection automatic by well-known port: `1710` → QRC (M2-M7 path, unchanged), `1702` → ECP. Zero new public-API surface.
- `EcpConnectionManager` + `EcpCommandQueue` — duplication-by-design from the M2 QRC `ConnectionManager` + `CommandQueue`, mirroring the state machine, reconnect cadence, and 3-role `ThreadCensus` registration (session, send, keepalive). Keepalive emits `sg` every 30 s of outbound silence.
- ECP `login_required` handshake — credentials supplied via `Initialize(host, port, username, password)`; on `login_failed` the manager triggers the standard 15 s reconnect cycle.
- Parallel ECP service tier (`EcpAudioControlService`, `EcpAudioRoutingService`, `EcpAudioZoneEnableService`, `EcpLogicTriggerService`) — emits `csv` / `css` / `ct` / `ssl` against the existing M2-M7 registries; reuses the `LevelScaler` and `AudioChannelRegistry` unchanged.
- `FakeEcpServer` in `TestSupport/Fakes/` — in-process TCP listener that speaks ECP per `research/ECP_PROTOCOL.md`. Fault-injection knobs: `SetActive`, `RespondWithCoreNotActive`, `EmitMalformed`, `RequireLogin`. Used by the new integration suite.

### Changed

- `QscDspTcp.SetBackupDeviceConnection`: now refuses mixed-protocol pairs with `Logger.Error`. Same-protocol ECP pairs refuse with `Logger.Notice` and a deferral to M-ECP-part-3 (sg-poll integration with `RedundantConnectionPair`).

### Test coverage

- 91 new unit tests (framer, quoting, commands, responses, dispatcher, queue, connection manager, audio control, redundancy refusal).
- 3 new integration tests against `FakeEcpServer` (sg round-trip, csv echo, login handshake).
- All 420 unit + 19 integration + 15 property tests green.

### Deferred

- M-ECP-part-3: `EcpRedundantConnectionPair` (sg-polling translation into `RedundantConnectionPair`'s `EngineState` consumer). Covered design-wise in `openspec/changes/.../design.md` §D-E3.

## [M7 Hardening] — `2026-05-04-add-hardening-and-final-docs`

### Fixed

- **Issue #14**: `QscDspTcp` moved from `QscDspDevices.Plugin` to the
  root `QscDspDevices` namespace. The framework loader resolves the
  entry point by FQN; the wrong namespace would have failed to load
  against the real GCU package at delivery time.

### Added

- `DisconnectCleanup.TryEnqueueDestroy` — best-effort `ChangeGroup.Destroy`
  on graceful disconnect (single-Core and redundant paths).
- `LogRedaction.Render(JsonRpcRequest)` — log-safe payload render that
  redacts `password` (case-insensitive, including nested) without
  mutating on-wire bytes.
- `PublicSurfaceTests` — reflection-based snapshot lock for the public
  API of `QscDspDevices.dll`. Snapshot lives at
  `tests/QscDspDevices.UnitTests/PublicSurface.expected.txt`.
- Two M6 integration tests previously deferred: switchback to primary
  on its return to Active, and double-Standby refusal.
- Five FsCheck properties: three on `SwitchbackPolicy.PickActive`,
  two on `RoutingCommandQueue`.
- `ConnectionManagerTests.Connected_steady_state_registers_three_threadcensus_roles`
  pinning the README §4 "≤ 3 concurrent threads" budget.

### Changed

- CI line-coverage gate: 90 % → 91 % (M7 baseline).
- Threading-budget spec walks back the M2-era over-prescription that
  required OS `Thread` instances and named send/recv/timer roles. The
  README's literal rule is "≤ 3 concurrent threads" — the current
  task-loop shape (`session`, `send`, `keepalive` registered with
  `ThreadCensus`) satisfies it. Recorded as deviation D-T1.
- Cross-suite parallel-test stability: integration tests now wait for
  `IsAccepting` (not just `Connected`) before enqueueing, since
  `ConnectionManager` fires `StateChanged(Connected)` *before* it
  calls `_queue.StartAccepting()`. Wall-clock waits bumped to 30 s
  to absorb threadpool starvation under load.

### Deferred

- Mid-session `AddInputChannel` / `AddOutputChannel` / `AddPreset` /
  `AddAudioZoneEnable` / `AddDspLogicTrigger` subscribe-on-the-wire
  → M-ECP. Today the registry add is staged and applied on next
  hydration, which is fine for the framework's actual call pattern
  (configuration before Connect).
- Per-symbol `public` → `internal` reductions → M-ECP.
- Stryker mutation testing → post-M7. Property tests already raise the
  bar where mutation testing would target.

## [M6] — Redundancy / Failover (archived 2026-05-04)

### Added

- `RedundantConnectionPair` coordinator, `RoutingCommandQueue` facade,
  `EngineStatusObserver`, `SwitchbackPolicy`.
- `QscDspTcp.SetBackupDeviceConnection(host, port)` activates the
  redundant path. Default switchback is README-conformant (return to
  primary on Active); QSC's official sticky-on-current guidance is
  opt-in via `SwitchbackPolicy.QscRecommended`.

## [M5] — Logic Triggers (archived 2026-05-02)

### Added

- `LogicTriggerRegistry` + `LogicTriggerService`. `IDspLogicTriggerSupport`
  surface on `QscDspTcp`.

## [M4] — Audio Routing + Zones (archived 2026-05-02)

### Added

- `AudioRoutingService`, `AudioZoneEnableService`. `IAudioRoutable`
  and `IAudioZoneEnabler` surfaces on `QscDspTcp`.

## [M3] — Audio Control + Presets (archived 2026-05-02)

### Added

- `AudioChannelRegistry`, `AudioControlService`, `LevelScaler`.
  Hydrate-via-`ChangeGroup.AddComponentControl` post-connect chain.
  AutoPoll fanout dispatch for inbound deltas.

## [M2] — QRC Client + Connection Manager (archived 2026-05-01)

### Added

- `QrcFramer`, `JsonRpcDispatcher`, `CommandQueue`, `ConnectionManager`,
  `ReconnectStrategy`, `KeepaliveTimer`, `ThreadCensus`. Integration
  tests against in-process `FakeQrcServer`.

## [M1] — Project Foundation (archived 2026-04-30)

### Added

- Solution, projects, framework stubs, CI workflow, OpenSpec setup,
  `qsc-critic` subagent.
