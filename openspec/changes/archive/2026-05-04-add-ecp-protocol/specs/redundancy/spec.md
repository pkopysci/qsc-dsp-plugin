# Redundancy — Spec Delta (M-ECP)

## ADDED Requirements

### Requirement: Redundant pairs are single-protocol

A redundant pair built via `SetBackupDeviceConnection` MUST use the same protocol on both sides. The plugin SHALL refuse a mixed-protocol pair (port `1710` primary + port `1702` backup, or vice versa) with `Logger.Error("redundant pair must use same protocol on both sides; call refused")` and a no-op return — the backup transport MUST NOT be constructed.

#### Scenario: ECP backup against a QRC primary is refused

- **GIVEN** `Initialize("10.0.0.1", 1710, …)` configured a QRC primary
- **WHEN** `SetBackupDeviceConnection("10.0.0.2", 1702)` is called
- **THEN** `Logger.Error` is emitted naming the protocol mismatch
- **AND** `BackupDeviceExists` remains `false`

#### Scenario: QRC backup against an ECP primary is refused

- **GIVEN** `Initialize("10.0.0.1", 1702, …)` configured an ECP primary
- **WHEN** `SetBackupDeviceConnection("10.0.0.2", 1710)` is called
- **THEN** `Logger.Error` is emitted naming the protocol mismatch
- **AND** `BackupDeviceExists` remains `false`
