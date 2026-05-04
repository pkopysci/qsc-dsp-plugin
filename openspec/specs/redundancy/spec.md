# redundancy Specification

## Purpose
TBD - created by archiving change add-redundancy. Update Purpose after archive.
## Requirements
### Requirement: SetBackupDeviceConnection promotes the plugin to a redundant pair

When the framework calls `SetBackupDeviceConnection(hostname, port)` after `Initialize` and before `Connect`, the plugin SHALL stash the backup config and, on the subsequent `Connect()`, construct a `RedundantConnectionPair` containing two `ConnectionManager` instances (primary + backup). When `SetBackupDeviceConnection` is NOT called, `Connect()` MUST construct the M5 single-manager stack unchanged. Per the framework spec, `BackupDeviceExists` returns `true` from the moment `SetBackupDeviceConnection` returns successfully, regardless of whether either Core has been Connected yet.

#### Scenario: SetBackupDeviceConnection sets BackupDeviceExists true

- **GIVEN** a freshly Initialized `QscDspTcp` with `BackupDeviceExists == false`
- **WHEN** the framework calls `SetBackupDeviceConnection("backup.example.com", 1710)`
- **THEN** `BackupDeviceExists` returns `true`
- **AND** `Connect()` constructs the redundant pair

### Requirement: Active connection is the one whose latest EngineStatus.State is Active

The `RedundantConnectionPair` SHALL track the most recent `EngineStatus.State` push observed on each connection. The "active" connection at any moment SHALL be the one most recently observed reporting `State == "Active"`. Routing of outbound writes (via `RoutingCommandQueue`) MUST follow this active-connection pointer. When neither connection is reporting `Active` (initial pre-EngineStatus window, or both `Standby` / `Idle`), writes MUST be refused with `Logger.Error` from the queue facade.

#### Scenario: First Active push selects that connection

- **GIVEN** the pair is Connecting; neither connection has reported a State yet
- **WHEN** the primary's `EngineStatus { State: "Active" }` push arrives
- **THEN** the active connection is the primary
- **AND** `RoutingCommandQueue.TryEnqueue` succeeds against the primary's queue

### Requirement: Failover when active reports Standby or drops the socket

When the currently-active connection's most recent `EngineStatus` push reports `State != "Active"`, OR its TCP socket transitions out of Connected, the pair coordinator SHALL re-evaluate and (if the other connection is Active) switch the routing target. The switchover MUST trigger the post-connect chain on the newly-active manager (Logon if creds configured, then change-group hydrate). Writes MUST resume only after the new connection's hydrate completes; until then `RoutingCommandQueue` continues to refuse + log.

#### Scenario: Primary pushes Standby with backup already Active triggers failover

- **GIVEN** the active connection is the primary and the backup's most recent push was `Active`
- **WHEN** the primary pushes `EngineStatus { State: "Standby" }`
- **THEN** the active connection becomes the backup
- **AND** the post-connect chain runs on the backup (Logon if creds; hydrate)
- **AND** `RedundancyStateChanged` fires once

### Requirement: Switchback to primary when it returns to Active

When `RespectQscFailoverGuidance == false` (the default per README), the pair SHALL switch the active connection back to the primary the next time the primary pushes `EngineStatus { State: "Active" }` after a failover. When `RespectQscFailoverGuidance == true`, the pair SHALL NOT switch back; the active stays on whichever connection most recently reported `Active` until a different transition happens.

#### Scenario: Default policy switches back to primary on its return-to-Active

- **GIVEN** the pair has failed over to backup and `RespectQscFailoverGuidance == false`
- **WHEN** the primary pushes `EngineStatus { State: "Active" }`
- **THEN** the active connection becomes the primary
- **AND** `RedundancyStateChanged` fires once

#### Scenario: QSC-guidance policy stays on backup until backup itself transitions

- **GIVEN** the pair has failed over to backup and `RespectQscFailoverGuidance == true`
- **WHEN** the primary pushes `EngineStatus { State: "Active" }` (while backup is still Active too)
- **THEN** the active connection stays on the backup

### Requirement: BackupDeviceConnectionChanged fires on backup TCP up and down

The pair SHALL fire `BackupDeviceConnectionChanged` once whenever the backup connection's `ConnectionState` transitions into `Connected` or out of `Connected`. The Single-arg event carries the device id per the framework spec. The primary connection's TCP transitions are reported via the existing `BaseDevice.IsOnline` + `NotifyOnlineStatus` channel (M2 contract); only the backup uses this dedicated event.

#### Scenario: Backup socket drop fires the event

- **GIVEN** both primary and backup are Connected
- **WHEN** the backup's TCP socket drops
- **THEN** `BackupDeviceConnectionChanged` fires once

### Requirement: PrimaryDeviceActive and BackupDeviceActive are mutually exclusive

`PrimaryDeviceActive` returns `true` iff the active connection is the primary. `BackupDeviceActive` returns `true` iff the active connection is the backup. Both MUST be `false` when no connection is currently routing writes (initial pre-EngineStatus window, both Standby, or both disconnected). They MUST never be `true` simultaneously.

#### Scenario: Pre-EngineStatus window reports both false

- **GIVEN** the pair is Connecting and no EngineStatus push has arrived
- **THEN** `PrimaryDeviceActive` is `false` AND `BackupDeviceActive` is `false`

#### Scenario: Failover transitions properties atomically

- **GIVEN** `PrimaryDeviceActive == true` AND `BackupDeviceActive == false`
- **WHEN** failover happens
- **THEN** the next observation reports `PrimaryDeviceActive == false` AND `BackupDeviceActive == true`

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

