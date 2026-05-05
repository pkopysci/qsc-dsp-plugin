# ecp-protocol Specification

## Purpose
TBD - created by archiving change add-ecp-protocol. Update Purpose after archive.
## Requirements
### Requirement: ECP wire framing

The plugin SHALL frame outbound ECP commands by appending `\n` (0x0A) to each command text. Inbound parsing SHALL split on `\n`, strip an optional trailing `\r` (0x0D) from each line, and ignore empty lines. Frames exceeding the configured maximum (default 64 KB) MUST raise `FrameTooLargeException`.

#### Scenario: Outbound command appends a single LF

- **GIVEN** a client emits the command text `"sg"`
- **WHEN** the framer serializes it
- **THEN** the bytes written to the transport are exactly `0x73 0x67 0x0A` (`sg\n`)

#### Scenario: Inbound CRLF response is parsed without the CR

- **GIVEN** the transport delivers `sr "Design" "Id" 1 1\r\n` as a single buffer
- **WHEN** the framer extracts the next frame
- **THEN** the frame text is `sr "Design" "Id" 1 1` (no trailing `\r`)

### Requirement: ECP string quoting

The plugin SHALL escape `\n`, `\r`, `"`, and `\` per the ECP §1.3 escape table when serializing any quoted string parameter. Unescape MUST be the round-trip inverse for any UTF-16 input.

#### Scenario: Round-trip quoting on a control name with quotes and newlines

- **GIVEN** a control name `My"Test\nControl`
- **WHEN** the value is escaped, written to the wire, parsed back, and unescaped
- **THEN** the recovered string equals the original input byte-for-byte

### Requirement: ECP protocol selection by well-known port

When the framework calls `Initialize(host, port, …)` with `port == 1710`, the plugin SHALL stand up the QRC stack. With `port == 1702`, it SHALL stand up the ECP stack. With any other port, it SHALL stand up the QRC stack and emit `Logger.Notice("non-standard port; assuming QRC — call SetEcpProtocol() if this is an ECP Core")`.

#### Scenario: Port 1710 routes to QRC

- **GIVEN** an integrator calls `Initialize("10.0.0.1", 1710, "user", "pin")`
- **WHEN** the plugin selects a backend
- **THEN** the QRC stack is constructed and the ECP stack is not

#### Scenario: Port 1702 routes to ECP

- **GIVEN** an integrator calls `Initialize("10.0.0.1", 1702, "user", "pin")`
- **WHEN** the plugin selects a backend
- **THEN** the ECP stack is constructed and the QRC stack is not

### Requirement: ECP keepalive cadence

The plugin SHALL send `sg` every 30 s of outbound silence on an ECP connection. A `cgsna` poll counts as outbound traffic and resets the silence timer.

#### Scenario: 30 seconds of silence emits sg

- **GIVEN** an ECP-connected plugin with no other commands sent for 30 s
- **WHEN** the keepalive timer fires
- **THEN** the next outbound frame is `sg\n`

### Requirement: ECP authentication via login_required banner

When an ECP Core sends `login_required\r\n` immediately after TCP accept (or in reply to the first command), the plugin SHALL respond with `login NAME PIN\n` from the credentials supplied at `Initialize`. On `login_failed\r\n` the plugin SHALL log `Logger.Error` and let the M2 reconnect cycle take over. On `login_success\r\n` or anonymous-mode (no banner), the connection proceeds.

#### Scenario: Login_required banner is honored

- **GIVEN** the Core sends `login_required\r\n` after TCP accept
- **AND** `Initialize` was called with `username = "admin"`, `password = "1234"`
- **WHEN** the connection adapter receives the banner
- **THEN** the next outbound frame is `login admin 1234\n`

#### Scenario: Login_failed triggers reconnect

- **GIVEN** the Core sends `login_required\r\n` then `login_failed\r\n`
- **WHEN** the adapter processes both lines
- **THEN** `Logger.Error` is emitted with a message containing "ECP login_failed"
- **AND** the connection transitions to `Disconnecting` and then begins the 15 s reconnect cadence

### Requirement: ECP service tier honours framework surface within ECP's expressible subset

Every public operation the framework surface exposes (`Set/GetAudio*Level`, `Set/GetAudio*Mute`, `RecallAudioPreset`, `RouteAudio`, `ClearAudioRoute`, `Set/Toggle/QueryAudioZoneEnable`, `PulseDspLogicTrigger`) is expressible in ECP because the framework's M3-M5 contract requires every routable / triggerable / mutable control to register a named-control tag (routerTag, levelTag, muteTag, zone-enable controlTag, trigger tagName). The ECP backend SHALL service every framework call by emitting the corresponding wire command (`csv` / `css` / `csp` / `ct` / `ssl`) against the registered named tag.

Direct matrix-crosspoint-by-index addressing is NOT exposed by the M3-M5 framework surface (the registries enforce a named-control tag), so it cannot reach the ECP backend and does not require a runtime guard or fallback log.

Component enumeration is similarly not exposed by the M3-M5 framework surface — reserved for a hypothetical future feature.

#### Scenario: ECP RouteAudio honours the registered router tag

- **GIVEN** the plugin is running over ECP
- **AND** an output `out1` is registered with `routerTag = "Mixer.input.1.gain"`
- **AND** an input `in3` is registered with `bankIndex = 3`
- **WHEN** `RouteAudio("in3", "out1")` is called
- **THEN** the next outbound ECP frame is `csv "Mixer.input.1.gain" 3\n`

### Requirement: ECP redundant pairs are refused at SetBackupDeviceConnection

Mixed-protocol pairs (one side QRC, the other ECP) MUST be refused at `SetBackupDeviceConnection` with `Logger.Error("redundant pair must use same protocol on both sides; call refused")`.

Same-protocol ECP pairs (both sides on port 1702) are also refused in this milestone with `Logger.Notice` — the implementation is tracked as M-ECP-part-3 (sg-poll integration with `RedundantConnectionPair` requires either widening the pair's types or building a parallel `EcpRedundantConnectionPair`; both choices have nontrivial scope and are deferred). When wired in M-ECP-part-3, the implementation will poll `sg` every 2 s on each side, translate `IS_ACTIVE` into the M6 `EngineState.Active` / `Standby` values, and reuse the existing `SwitchbackPolicy`.

Single-Core ECP (no `SetBackupDeviceConnection`) is unaffected and supports the full M3-M5 feature subset.

#### Scenario: Mixed-protocol pair is refused with Error

- **GIVEN** the plugin called `Initialize("10.0.0.1", 1710, …)` (QRC primary)
- **WHEN** the integrator calls `SetBackupDeviceConnection("10.0.0.2", 1702)` (ECP backup)
- **THEN** `Logger.Error` is emitted with text containing "redundant pair must use same protocol"
- **AND** no backup connection is constructed

#### Scenario: Same-protocol ECP pair is refused with Notice (M-ECP-part-2 limitation)

- **GIVEN** the plugin called `Initialize("10.0.0.1", 1702, …)` (ECP primary)
- **WHEN** the integrator calls `SetBackupDeviceConnection("10.0.0.2", 1702)` (ECP backup)
- **THEN** `Logger.Notice` is emitted naming "M-ECP-part-3"
- **AND** no backup connection is constructed

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

