# threading-budget Specification

## Purpose
TBD - created by archiving change add-qrc-client-and-connection. Update Purpose after archive.
## Requirements
### Requirement: At most three plugin-owned threads alive simultaneously

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

### Requirement: ThreadCensus runtime guard logs and (in DEBUG) fails fast on a 4th thread

The plugin SHALL implement a `ThreadCensus` that registers each plugin-owned thread on start and unregisters on exit. If a fourth registration is attempted while three are alive, the census MUST log `Logger.Error` "thread budget breached: 4 plugin threads alive" with the names. In DEBUG builds it SHALL additionally call `Environment.FailFast` so test runs surface the breach immediately. RELEASE builds log and continue (the README forbids host crashes).

#### Scenario: Test attempts to register a 4th plugin thread

- **GIVEN** a unit test that simulates 3 plugin threads already registered
- **WHEN** a 4th registration is attempted
- **THEN** in DEBUG, `Environment.FailFast` is invoked
- **AND** in RELEASE, a `Logger.Error` entry is recorded and the registration is refused

### Requirement: No public async surface

The public surface of `QscDspTcp` and every interface implemented by it MUST NOT expose `Task`, `ValueTask`, or `async`-marked methods. Public mutators MUST return synchronously and complete their visible state changes either inline or via subsequent events. Internal `Task`/`async` use is permitted for thread-loop plumbing.

#### Scenario: SetAudioOutputLevel returns synchronously

- **GIVEN** the plugin in any state
- **WHEN** `SetAudioOutputLevel("out1", 50)` is called
- **THEN** the call returns synchronously (no `Task` returned, no `await` required by the caller)

### Requirement: IQrcClock abstraction for deterministic test timing

All time-aware components (KeepaliveTimer, ReconnectStrategy) SHALL accept an `IQrcClock` constructor parameter. Production wires `SystemClock` (uses `DateTime.UtcNow` and `Task.Delay`); tests wire `DeterministicClock` from TestSupport (manually-advanced virtual time). No time-aware code SHALL call `DateTime.Now`, `DateTime.UtcNow`, `Thread.Sleep`, or `Task.Delay` directly.

#### Scenario: Test advances virtual time without waiting

- **GIVEN** a KeepaliveTimer constructed with a `DeterministicClock`
- **WHEN** the test calls `clock.Advance(TimeSpan.FromSeconds(30))`
- **THEN** the test observes the NoOp emission within milliseconds of wall-clock time

### Requirement: No two plugin threads hold more than one lock at a time

To prevent deadlock, the plugin's design SHALL ensure no thread holds more than one of its internal locks (`CommandQueue._stateLock`, `ChangeGroupManager._lock`, `AudioChannelRegistry._lock`, `ThreadCensus._lock`) at any given instant, and no thread SHALL block waiting on another thread's lock while holding its own. This rule is enforced by code-review convention; there is no runtime detector.

#### Scenario: A producer enqueueing while a consumer drains does not deadlock

- **GIVEN** the qsc-send thread is draining CommandQueue under `_stateLock`
- **WHEN** a producer thread calls `TryEnqueue` from outside (framework call site)
- **THEN** the producer waits on the same lock and does not hold any other plugin lock concurrently

