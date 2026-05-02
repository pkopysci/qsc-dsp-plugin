# Connection Manager — Spec Delta

## ADDED Requirements

### Requirement: Single-Core deployments retain unchanged M5 behaviour

When `SetBackupDeviceConnection` was NOT called, the plugin SHALL construct the M5 single-`ConnectionManager` stack on `Connect()` exactly as it did before M6. The redundant-pair code path MUST NOT be exercised in any way; service tier consumers (M3-M5 cache services) MUST continue to enqueue against a plain `CommandQueue`, not the `RoutingCommandQueue` facade.

#### Scenario: Single-Core Connect produces no backup overhead

- **GIVEN** `Initialize` ran but `SetBackupDeviceConnection` was not called
- **WHEN** `Connect()` is called
- **THEN** `BackupDeviceExists` returns `false`
- **AND** the M5 unit-test surface (e.g. `ThreadCensus.Snapshot()` containing exactly `session`, `send`, `keepalive`) is unchanged
