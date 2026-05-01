# Threading Budget — Spec Delta

## ADDED Requirements

### Requirement: At most three plugin-owned threads alive simultaneously

The plugin SHALL operate within a hard cap of three concurrently-alive plugin-owned threads, per README §4. The three threads have fixed roles: send-loop, receive-loop, timer. Any code path that would spawn a fourth thread MUST be considered a defect.

#### Scenario: Steady-state plugin reports 3 plugin threads

- **GIVEN** a Connected plugin in steady state
- **WHEN** the ThreadCensus is queried
- **THEN** exactly 3 plugin-owned threads are reported

#### Scenario: Disconnected plugin reports 0 plugin threads

- **GIVEN** a plugin that has completed `Disconnect()`
- **WHEN** the ThreadCensus is queried
- **THEN** zero plugin-owned threads are reported

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
