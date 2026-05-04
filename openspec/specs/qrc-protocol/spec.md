# qrc-protocol Specification

## Purpose
TBD - created by archiving change add-qrc-client-and-connection. Update Purpose after archive.
## Requirements
### Requirement: Null-byte framed JSON-RPC 2.0 over UTF-8

The QRC framer SHALL split the inbound TCP byte stream on the ASCII NUL byte (`0x00`), interpret each delimited segment as a UTF-8-encoded JSON-RPC 2.0 message, and emit one frame per delimiter. Outbound encoding SHALL serialize a JSON-RPC message as UTF-8 and append exactly one `0x00` terminator.

#### Scenario: Two complete frames in one read

- **GIVEN** the transport delivers `<json1>\x00<json2>\x00` in a single buffer
- **WHEN** the framer processes the buffer
- **THEN** two JSON frames are emitted in order

#### Scenario: One frame split across two reads

- **GIVEN** the transport delivers `<half1>` then `<half2>\x00`
- **WHEN** both buffers are processed
- **THEN** exactly one JSON frame is emitted, equal to `<half1><half2>`

### Requirement: Frame size is bounded; oversize frames force reconnect

The framer SHALL read into a growable buffer that starts at 16 KiB and doubles up to a configurable maximum (default 16 MiB). A single frame whose accumulated bytes exceed the maximum SHALL cause the framer to log `Logger.Error`, raise `FrameTooLargeException`, and signal the connection manager to drop and reconnect.

#### Scenario: 17 MiB frame triggers reconnect

- **GIVEN** the configured max-frame-size is 16 MiB
- **WHEN** the framer accumulates 17 MiB of inbound bytes with no `0x00`
- **THEN** the framer raises `FrameTooLargeException`
- **AND** the connection manager observes the fault and drops the connection

### Requirement: Strongly-typed JSON-RPC models

The plugin SHALL define `JsonRpcRequest`, `JsonRpcResponse`, `JsonRpcError`, and `JsonRpcNotification` POCOs serializable via `Newtonsoft.Json` (the only allowed JSON library per the README's NuGet whitelist). The version field MUST always be the string literal `"2.0"`.

#### Scenario: Round-trip a Component.Set request

- **WHEN** a `JsonRpcRequest` for `method = "Component.Set"` is serialized to JSON, written through the framer, parsed back via the framer, and deserialized
- **THEN** the resulting object equals the original by value

### Requirement: Id-based request/response correlation with AutoPoll subscription map

The dispatcher SHALL maintain a concurrent dictionary of pending requests keyed by their monotonic int64 id. When a `JsonRpcResponse` arrives, the dispatcher SHALL first check whether the id is registered in the AutoPoll subscription map; if so, the response is delivered to the subscription's observer. Otherwise the matching pending request is completed. Ids of unknown responses SHALL log `Logger.Warn` and be discarded.

#### Scenario: AutoPoll-pushed update routes to the subscription, not the original waiter

- **GIVEN** a request `ChangeGroup.AutoPoll` with id 42 was sent and registered as an AutoPoll subscription
- **WHEN** a subsequent inbound response carries id 42 and a populated Changes array
- **THEN** the AutoPoll subscription's observer receives the Changes
- **AND** no pending request completes from this push

### Requirement: QSC error codes round-trip to a typed enum

The plugin SHALL define `QrcErrorCode` enumerating the standard JSON-RPC error codes plus the QSC-specific codes documented in the QRC protocol research: `-32604` (Standby), `5` (ChangeGroupsExhausted), `6` (UnknownChangeGroup), `7` (UnknownComponentName), `8` (UnknownControl), `9` (IllegalMixerChannelIndex), `10` (LogonRequired). Unknown error codes MUST log `Logger.Warn` and be classified as `ServerError`.

#### Scenario: Standby Core error is recognized

- **WHEN** a JsonRpcError with code -32604 is received
- **THEN** the typed code surfaces as `QrcErrorCode.CoreOnStandby` to the caller

### Requirement: Logon payload is redacted in debug logs

When the plugin emits a `Logger.Debug` log of an outbound `Logon` JSON-RPC request — whether from `LogonAction`, the `CommandQueue` send-loop, or the `JsonRpcDispatcher` outbound path — the `password` field of the request's `params` block MUST be replaced with the literal string `"***"` before the message is formatted. The on-wire payload MUST NOT be modified; only the logged representation is altered.

#### Scenario: Logon debug log shows redacted password

- **GIVEN** debug logging is enabled and `LogonAction` runs with `password = "hunter2"`
- **WHEN** the outbound `Logon` request is formatted for `Logger.Debug`
- **THEN** the logged JSON contains `"password":"***"`
- **AND** the bytes written to the transport contain `"password":"hunter2"`

