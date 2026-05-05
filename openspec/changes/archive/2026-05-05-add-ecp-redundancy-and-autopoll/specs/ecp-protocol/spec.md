# ECP Protocol — Spec Delta (M-ECP part 3)

## ADDED Requirements

### Requirement: ECP AutoPoll hydration on connect

After the `login_required` handshake completes (or when anonymous-mode is detected), the plugin SHALL emit a single `cgc 1` to create a change group, then `cga 1 "<tag>"` for every named control registered with `AudioChannelRegistry`, `AudioZoneRegistry`, and `LogicTriggerRegistry`, then `cgsna 1 2000` to start a 2-s no-ack poll. The connection MUST NOT transition to `Connected` until the hydrate sequence has been enqueued.

#### Scenario: Hydration emits cgc + cga + cgsna in order

- **GIVEN** an ECP connection that just completed authentication
- **AND** the plugin has registered named controls `Output.gain` (audio level), `Logic.button` (logic trigger), and `Zone.A.enable` (zone)
- **WHEN** the post-auth hydrate runs
- **THEN** the next outbound frames are exactly `cgc 1\n`, `cga 1 "Output.gain"\n`, `cga 1 "Logic.button"\n`, `cga 1 "Zone.A.enable"\n`, `cgsna 1 2000\n` (in any order for the cga lines, but cgc first and cgsna last)

### Requirement: ECP AutoPoll bridge updates the optimistic cache

When the Core emits a `cv` line via the 2-s `cgsna` schedule, the `EcpAutoPollSubscription` SHALL translate the `(controlId, value, position)` tuple into the appropriate service-tier cache update. If the inbound value disagrees with the existing optimistic cache, the cache MUST be corrected and the corresponding `AudioInputLevelChanged` / `AudioOutputLevelChanged` / `AudioRouteChanged` / `AudioZoneEnableChanged` / `DspLogicTriggerStateChanged` event MUST be re-raised with the authoritative value.

#### Scenario: cv update overrides optimistic level

- **GIVEN** the ECP audio-control service has optimistic-cached `out1.level = 50` from a prior `Set`
- **WHEN** an inbound `cv "Output.gain" "-6dB" -6 0.7` arrives
- **AND** the registered output `out1` resolves to the named control `Output.gain` with range `[-100, 0]`
- **THEN** the cache for `out1` is updated to the framework-side value derived from `-6` (somewhere around 94)
- **AND** `AudioOutputLevelChanged` is raised with the new value

### Requirement: ECP redundancy via sg-poll

Under ECP, the plugin SHALL schedule an `sg` query every 2 s on each connection of a redundant pair. The `sr` response's `IS_ACTIVE` field SHALL be translated into `EngineState.Active` (when `IS_ACTIVE=1`) or `EngineState.Standby` (when `IS_ACTIVE=0`) and fed to the M6 `RedundantConnectionPair` through the same `EngineState`-consuming seam the QRC `EngineStatusObserver` uses. The M6 `SwitchbackPolicy` and routing-queue rewiring are unchanged.

The `cgsna 1 2000` AutoPoll schedule from the hydrate path counts as outbound traffic and resets the keepalive silence window; the dedicated 2-s `sg` poll runs independently of it.

#### Scenario: Primary IS_ACTIVE flip triggers failover

- **GIVEN** an ECP redundant pair with primary `IS_ACTIVE=1` and backup `IS_ACTIVE=0`, currently routing to primary
- **WHEN** the next `sg` poll on the primary returns `IS_ACTIVE=0` and the next on the backup returns `IS_ACTIVE=1`
- **THEN** the pair coordinator promotes Backup
- **AND** subsequent writes route to the backup's command queue

### Requirement: Same-protocol ECP redundant pairs are constructed (not refused)

`SetBackupDeviceConnection` for an ECP primary (port 1702) with a backup also on port 1702 SHALL construct the redundant pair using the M6 `RedundantConnectionPair` coordinator wired against the per-side `EcpConnectionManager`, `EcpCommandQueue`, and `EcpEngineStateProbe`. The previous `Logger.Notice("...M-ECP-part-3")` refusal is REMOVED.

Mixed-protocol pairs (one side QRC, the other ECP) MUST still be refused with `Logger.Error` per the M-ECP-part-2 redundancy spec.

#### Scenario: ECP backup against an ECP primary is constructed

- **GIVEN** `Initialize("10.0.0.1", 1702, …)` configured an ECP primary
- **WHEN** `SetBackupDeviceConnection("10.0.0.2", 1702)` is called
- **THEN** `BackupDeviceExists` returns `true`
- **AND** no `Logger.Error` or `Logger.Notice` is emitted
