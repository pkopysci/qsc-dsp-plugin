# Threading Budget — Spec Delta

## MODIFIED Requirements

### Requirement: Plugin owns at most three live threads

The plugin SHALL never have more than three OS-level threads of its own alive simultaneously. M2 satisfied this trivially with a single threadpool task; M3 lays down the actual three-thread layout that every higher-milestone feature is built on: `qsc-send` (drains `CommandQueue` and writes to the transport), `qsc-recv` (owns the `_transport.RxBytesReceived` callback and drives `QrcFramer` plus `JsonRpcDispatcher`), and `qsc-timer` (runs the keepalive cadence — `NoOp` every 30 s of outbound silence — and the 15 s reconnect interval). Each thread MUST register with `ThreadCensus` on entry and dispose the registration on exit. The threads SHALL be created at `Connect()` time and joined at `Disconnect()` time. The M2 `RunSessionAsync` method MUST be removed.

#### Scenario: Steady-state Connected reports exactly 3 plugin threads

- **GIVEN** the plugin is Connected with hydration complete
- **WHEN** `ThreadCensus.Snapshot()` is called
- **THEN** the snapshot contains exactly three roles: `qsc-send`, `qsc-recv`, `qsc-timer`

#### Scenario: After Disconnect the census reports zero plugin threads

- **GIVEN** a Connected plugin with three threads alive
- **WHEN** Disconnect() is called and returns
- **THEN** `ThreadCensus.AliveCount` is 0

## ADDED Requirements

### Requirement: No two plugin threads hold more than one lock at a time

To prevent deadlock, the plugin's design SHALL ensure no thread holds more than one of its internal locks (`CommandQueue._stateLock`, `ChangeGroupManager._lock`, `AudioChannelRegistry._lock`, `ThreadCensus._lock`) at any given instant, and no thread SHALL block waiting on another thread's lock while holding its own. This rule is enforced by code-review convention; there is no runtime detector.

#### Scenario: A producer enqueueing while a consumer drains does not deadlock

- **GIVEN** the qsc-send thread is draining CommandQueue under `_stateLock`
- **WHEN** a producer thread calls `TryEnqueue` from outside (framework call site)
- **THEN** the producer waits on the same lock and does not hold any other plugin lock concurrently
