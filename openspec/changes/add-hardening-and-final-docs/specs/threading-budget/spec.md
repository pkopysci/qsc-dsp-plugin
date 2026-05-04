# Threading Budget — Spec Delta (M7)

## MODIFIED Requirements

### Requirement: At most three plugin-owned worker registrations per connection

The plugin SHALL register at most three steady-state worker roles with `ThreadCensus` per `ConnectionManager`. The registered roles are `qsc-send` (drains `CommandQueue` and writes to the transport), `qsc-recv` (drives the `_transport.RxReceived` callback through `QrcFramer` + `JsonRpcDispatcher`), and `qsc-timer` (keepalive cadence + reconnect interval).

These roles MAY be implemented as dedicated OS-level `Thread` instances OR as long-running `Task` loops scheduled on the threadpool. The choice is captured by deviation **D-T1** in `SPEC_COMPLIANCE.md`: M7 selects the Task-loop implementation because (a) the receive path is event-driven via `BasicTcpClient.RxReceived` and gains nothing from a dedicated `Thread`; (b) the M3 + M6 flake modes were post-connect-chain races, not threadpool starvation; (c) the M2 `RunSessionAsync` orchestrator does not register itself, so the registered count remains exactly three.

The orchestrator (`RunSessionAsync`) is permitted to remain in M7 — the previous version of this requirement called for its removal; that requirement is superseded by **D-T1**.

#### Scenario: Steady-state Connected reports exactly 3 plugin worker registrations

- **GIVEN** the plugin is Connected with hydration complete
- **WHEN** `ThreadCensus.Snapshot()` is called
- **THEN** the snapshot contains exactly three roles: `qsc-send`, `qsc-recv`, `qsc-timer`

#### Scenario: After Disconnect the census reports zero plugin workers

- **GIVEN** a Connected plugin with three workers registered
- **WHEN** `Disconnect()` is called and returns
- **THEN** `ThreadCensus.AliveCount` is 0
