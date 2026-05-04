# Connection Manager — Spec Delta (M-ECP)

## ADDED Requirements

### Requirement: Per-protocol connection adapter

The plugin SHALL provide a connection-management implementation per supported protocol. Both implementations MUST share the M2 state-machine semantics (Disconnected → Connecting → Connected → Disconnecting → Disconnected), the M2 reconnect cadence (15 s after a failed attempt), and the M2 `ThreadCensus` 3-role registration (`session`, `send`, `keepalive`).

The QRC implementation is `ConnectionManager` (M2-M7). The ECP implementation is `EcpConnectionManager` (M-ECP). Both are constructable from `QscDspTcp.Initialize` based on the well-known port.

#### Scenario: ECP connection passes through the same state-machine sequence

- **GIVEN** a fresh `EcpConnectionManager` with a connectable transport
- **WHEN** `Connect()` is called
- **THEN** the observed state sequence is `Connecting → Connected`
- **AND** the connection registers exactly three `ThreadCensus` roles: `session`, `send`, `keepalive`

#### Scenario: ECP connection survives a transport drop with the M2 reconnect cadence

- **GIVEN** an `EcpConnectionManager` in `Connected`
- **WHEN** the transport reports a fault
- **THEN** the manager transitions to `Disconnected` and schedules a reconnect attempt 15 s later
