# Threading Budget — Spec Delta (M7)

## MODIFIED Requirements

### Requirement: At most three concurrent plugin-owned threads per connection

The plugin SHALL hold at most three concurrent plugin-owned threadpool tasks per `ConnectionManager`, as required by README §4 ("Internal thread count must be limitted to a maximum of 3 concurrent threads"). Each task SHALL register with `ThreadCensus` on entry and unregister on exit so the runtime guard can detect a breach. The registered roles are:

- `session` — the `RunSessionAsync` orchestrator that drives the connect / hydrate / disconnect state machine
- `send` — drains `CommandQueue` and writes framed JSON-RPC bytes to the transport
- `keepalive` — fires the 30 s `NoOp` cadence on outbound silence and the 15 s reconnect interval

The receive path is event-driven via `BasicTcpClient.RxReceived` and runs on the `BasicTcpClient`'s own callback thread. It is NOT a plugin-owned thread and does NOT count toward the budget.

These roles MAY be implemented as dedicated OS-level `Thread` instances OR as long-running `Task` loops scheduled on the threadpool. M7 selects the Task-loop implementation (recorded as deviation **D-T1** in `SPEC_COMPLIANCE.md`). D-T1 walks back the prior spec's prescription of literal `Thread` instances and named `qsc-send`/`qsc-recv`/`qsc-timer` roles — those names and the OS-Thread requirement were elaborations beyond the README's literal text.

#### Scenario: Steady-state Connected reports exactly 3 plugin-owned tasks

- **GIVEN** the plugin is Connected with hydration complete
- **WHEN** `ThreadCensus.Snapshot()` is called
- **THEN** the snapshot contains exactly three roles: `session`, `send`, `keepalive`

#### Scenario: After Disconnect the census reports zero plugin-owned tasks

- **GIVEN** a Connected plugin with three tasks registered
- **WHEN** `Disconnect()` is called and returns
- **THEN** `ThreadCensus.AliveCount` is 0
