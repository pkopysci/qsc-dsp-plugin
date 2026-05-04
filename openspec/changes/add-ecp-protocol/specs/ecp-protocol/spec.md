# ECP Protocol — Spec Delta (M-ECP)

## ADDED Requirements

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
