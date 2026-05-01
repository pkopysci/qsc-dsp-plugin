# Change: Add QRC client, connection manager, and 3-thread runtime

## Why

M1 established the build foundation, framework stubs, and CI gates. This
milestone adds the actual machinery the plugin will run on: a QRC
(Q-SYS Remote Control) client speaking JSON-RPC 2.0 over a null-byte
framed TCP stream, a connection manager that owns the socket lifecycle
and the README-mandated 15-second reconnect loop, a FIFO command queue
that drops on disconnect, and a strict 3-thread internal runtime budget.

Per `research/QRC_PROTOCOL.md`, the QRC protocol is plaintext on TCP/1710
with `\x00`-terminated JSON frames, a 60s server-side keepalive timeout,
and a hard limit of 4 change groups per connection. Every higher-level
feature (audio control in M3, routing in M4, logic triggers in M5,
redundancy in M6) is built on top of this milestone.

The deliverable also includes an in-process Fake QRC server in the
TestSupport project so unit, integration, and property tests can
exercise the full happy path plus failure-injection scenarios
(dropped frame, slow response, malformed JSON, sudden socket close,
keepalive miss, Standby-Core error -32604) without a real Q-SYS Core
in the loop.

This milestone does **not** implement the higher-level audio /
routing / preset / logic / redundancy / ECP features — those are M3
through M6 plus the ECP milestone. It also does **not** fully wire
those features into `QscDspTcp.Initialize`; this milestone delivers
the connection-and-command-pipe primitives that the later milestones
sit on.

## What Changes

- Add the public `QscDspTcp` class (`src/QscDspDevices/Plugin/QscDspTcp.cs`)
  inheriting from `gcu_hardware_service.BaseDevice.BaseDevice`. Sets
  `Manufacturer="QSC"` and `Model="Q-SYS Core"` per README §3. Overrides
  `Connect()` and `Disconnect()` to drive the connection manager.
  Exposes `Initialize(hostId, coreId, hostname, port, username, password)`
  required by `IDsp`. The audio-control, routing, logic, redundancy
  interface methods are present but empty / NotSupported until their
  respective milestones — this is explicit, documented, and makes the
  framework happy.
- Add `Transport/IConnectionTransport.cs` plus
  `Transport/BasicTcpClientTransport.cs` — a thin abstraction over the
  framework's `BasicTcpClient` (mandated by README §4) that exposes a
  byte-oriented send/receive API to the protocol layer. Failure-injection
  hooks live behind a `Transport/IFaultInjector.cs` interface used only
  by tests.
- Add `Protocol/QrcFramer.cs` — null-byte (`\x00`) frame splitter and
  builder. UTF-8. 16 KiB initial read buffer; configurable max frame
  size (default 16 MiB; oversize frames raise a fatal error and force
  reconnect rather than allocate without bound).
- Add `Protocol/JsonRpc/*` — strongly-typed JSON-RPC 2.0 request,
  response, error, and notification models. Backed by `Newtonsoft.Json`
  (the only allowed JSON library per the README's whitelist).
- Add `Protocol/JsonRpcDispatcher.cs` — id-based response correlation,
  AutoPoll subscription map, server-originated notification routing
  (`EngineStatus`, `Logon` results, etc.), and the QSC error-code-to-
  enum mapping (`QrcErrorCode`).
- Add `Protocol/CommandQueue.cs` — bounded FIFO. While disconnected
  every `TryEnqueue` returns false and logs `Logger.Error`. On any
  disconnect transition the queue is cleared atomically. Property tests
  prove FIFO under random concurrent producers.
- Add `Protocol/KeepaliveTimer.cs` — periodic `NoOp` JSON-RPC every
  30 s of outbound silence (the spec's 60 s timeout has 30 s of slack
  built in this way).
- Add `Connectivity/ConnectionManager.cs` — state machine
  `Disconnected → Connecting → Connected → Disconnecting → Disconnected`
  with a deterministic 15-second reconnect loop honouring the README's
  literal requirement. Drives `BaseDevice.IsOnline` and calls
  `BaseDevice.NotifyOnlineStatus()` after every transition. On
  successful connect the manager enqueues a hydration sequence (empty
  for now; M3 fills it in).
- Add `Plugin/Threading/ThreadCensus.cs` — runtime guard that registers
  every thread the plugin spawns and fails fast (in DEBUG; logs
  `Logger.Error` in RELEASE) if more than 3 are alive simultaneously.
  Threads: send-loop, receive-loop, timer.
- Add `tests/QscDspDevices.TestSupport/FakeQrcServer.cs` — TCP listener
  that speaks the QRC protocol per §15 of the QRC research doc. Emits
  `EngineStatus` immediately on connect. Implements `Logon`, `NoOp`,
  `Component.Set/Get`, `Control.Set/Get`, `ChangeGroup.*`,
  `Snapshot.Load`. Failure-injection hooks toggleable via test
  fixtures: `DropConnection()`, `DelayResponseMs(int)`,
  `EmitMalformed()`, `RespondWithStandbyError()`,
  `WithdrawKeepaliveTolerance()`.
- Add `tests/QscDspDevices.TestSupport/DeterministicClock.cs` —
  `IQrcClock` implementation tests can advance manually so reconnect
  timing is exercised in milliseconds, not seconds.
- Add unit tests covering: framer round-trip + fragmentation +
  oversize rejection, dispatcher id-correlation + AutoPoll routing,
  command-queue refusal-while-disconnected and clear-on-disconnect,
  keepalive emission timing, connection-manager state transitions.
- Add property-based tests (FsCheck) for the framer (random
  byte streams produce no exceptions, only deserialization errors)
  and command-queue FIFO under random interleaving.
- Add integration tests against the FakeQrcServer covering: connect
  success, connect failure → 15s retry, mid-flight socket drop →
  retry until success, queue drained on success, queue dropped on
  disconnect, 30s NoOp emitted under outbound silence.
- Add the qsc-critic review of M2 to
  `openspec/changes/add-qrc-client-and-connection/REVIEW.md` before
  the PR is opened.
- Update `SPEC_COMPLIANCE.md` rows that this milestone discharges:
  3.2-3.4, 4.1, 4.3, 7.1-7.4, 8.1-8.5 move from ⏳ to ✅ with file:line
  citations.
- Update `ARCHITECTURE.md` with the actual state-machine diagram,
  thread census details, and lock-order specifics now that the code
  exists.

## Impact

- Affected specs: NEW capabilities `qrc-protocol`, `transport`,
  `connection-manager`, `command-queue`, `threading-budget`.
- Affected code: new Plugin/, Transport/, Protocol/, Connectivity/
  trees in `src/QscDspDevices/`. New FakeQrcServer + DeterministicClock
  in `tests/QscDspDevices.TestSupport/`. New tests across all three
  test projects.
- The shipped DLL grows from 4.6 KB (empty body) to its real
  M2 size — well within the 500 KB budget. CI verifies.
- Coverage: reaches the 90% threshold against everything new
  (existing-but-unused code in M2 is exercised by integration tests
  via the connection lifecycle).

## Out of scope (deferred)

- `IAudioControl.AddInputChannel` / `AddOutputChannel` /
  `SetAudioInputLevel` etc. — M3.
- Matrix routing crosspoints — M4.
- Logic triggers — M5.
- Primary/backup redundancy with auto-switchback — M6.
- ECP backend — own milestone (M-ECP).
- TLS — QRC has no TLS port per QSC's published protocol; the
  transport abstraction is designed for future TLS pluggability but
  none ships in M2 (see deviation D1 in `SPEC_COMPLIANCE.md`).
- Hardware-validation runner — M7.
