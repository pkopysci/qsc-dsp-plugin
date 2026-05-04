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

### Requirement: ECP feature-gap operations log Logger.Notice

Operations whose semantics are not expressible in ECP SHALL log `Logger.Notice` with the message format `"<operation> requires QRC; ignored under ECP"` and return the documented fallback (false / no-op). The plugin MUST NOT throw for an unsupported-under-ECP operation.

The unsupported operations under ECP are:

- `SetAudioRoute` against a matrix crosspoint addressed by index (rather than by named control)
- Direct component enumeration via the framework surface (none of the M3-M5 services expose this today; reserved)

#### Scenario: SetAudioRoute by matrix index logs Notice and returns false

- **GIVEN** the plugin is running over ECP
- **AND** the `outputId` resolves to a matrix-crosspoint-by-index registration (no named-control alias)
- **WHEN** `SetAudioRoute(outputId, inputId)` is called
- **THEN** `Logger.Notice` is emitted with text containing "requires QRC"
- **AND** the call returns false
- **AND** no ECP frame is enqueued

### Requirement: ECP redundancy probes via sg poll

Under ECP, the plugin SHALL poll `sg` every 2 s on each side of a redundant pair to translate `IS_ACTIVE` into the `EngineState.Active` / `EngineState.Standby` values that `RedundantConnectionPair` consumes. The pair coordinator and switchback policy from M6 are unchanged.

Mixed-protocol pairs (one side QRC, the other ECP) MUST be refused at `SetBackupDeviceConnection` with `Logger.Error("redundant pair must use same protocol on both sides; call refused")`.

#### Scenario: Primary IS_ACTIVE flip triggers failover

- **GIVEN** an ECP redundant pair with primary `IS_ACTIVE=1` and backup `IS_ACTIVE=0`
- **AND** the pair is in `ActiveSlot=Primary`
- **WHEN** the next `sg` poll on the primary returns `IS_ACTIVE=0` and the next poll on the backup returns `IS_ACTIVE=1`
- **THEN** the policy promotes Backup
- **AND** subsequent writes route to the backup's command queue

#### Scenario: Mixed-protocol pair is refused

- **GIVEN** the plugin called `Initialize("10.0.0.1", 1710, …)` (QRC primary)
- **WHEN** the integrator calls `SetBackupDeviceConnection("10.0.0.2", 1702)` (ECP backup)
- **THEN** `Logger.Error` is emitted with text containing "redundant pair must use same protocol"
- **AND** no backup connection is constructed
