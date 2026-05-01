# command-queue Specification

## Purpose
TBD - created by archiving change add-qrc-client-and-connection. Update Purpose after archive.
## Requirements
### Requirement: FIFO command queue refuses while disconnected and clears on every disconnect

The plugin SHALL provide a FIFO command queue. While the connection state is anything other than `Connected`, every `TryEnqueue(command)` call MUST return false and MUST log `Logger.Error` "command attempted while disconnected". On every transition into `Disconnected` the queue MUST be atomically drained.

#### Scenario: Enqueue while disconnected is refused with an error log

- **GIVEN** the connection state is `Disconnected`
- **WHEN** `TryEnqueue(cmd)` is called
- **THEN** the call returns false
- **AND** a `Logger.Error` entry is recorded matching "command attempted while disconnected"

#### Scenario: Disconnect drains a non-empty queue

- **GIVEN** the queue contains 5 commands while Connected
- **WHEN** the connection transitions into `Disconnected`
- **THEN** the queue is empty
- **AND** the dropped count is logged at `Logger.Notice`

### Requirement: Bounded queue with oldest-drop saturation policy

The queue SHALL be bounded at 1024 outstanding commands. When the bound is reached and a new command arrives, the oldest queued command MUST be discarded, the new command MUST be enqueued, and `Logger.Warn` "command queue saturated; oldest command dropped" MUST be logged.

#### Scenario: Queue saturation drops the oldest command

- **GIVEN** the queue is full (1024 entries) and accepting commands
- **WHEN** a new TryEnqueue arrives
- **THEN** the entry that was at position 0 is removed
- **AND** the new entry is at position 1023
- **AND** a `Logger.Warn` entry mentions saturation

### Requirement: FIFO order is preserved under sequential enqueue

For any sequence of `TryEnqueue(cmd_i)` calls i = 1..N from a single thread while the queue is accepting and not saturated, the dequeue order observed by the send-loop MUST equal the enqueue order. This invariant is verified by a property test (FsCheck) using random N up to 1024.

#### Scenario: Dequeue order matches enqueue order

- **GIVEN** the queue is empty and accepting
- **WHEN** a single thread enqueues 100 commands tagged 1..100
- **AND** a single thread dequeues 100 times
- **THEN** the dequeued tags form the sequence 1..100 in order

### Requirement: Keepalive emits NoOp every 30 seconds of outbound silence

The plugin SHALL emit a `NoOp` JSON-RPC request every 30 seconds during which no other outbound frame has been written. This safely sits inside the QRC server's 60-second silence-disconnect window. Emitting any other outbound frame MUST reset the keepalive timer.

#### Scenario: Silent for 30s emits NoOp

- **GIVEN** the deterministic clock starts at t=0 and the connection is Connected with no outbound activity
- **WHEN** the clock advances to t=30s
- **THEN** exactly one `NoOp` request has been written by the send-loop

#### Scenario: Outbound activity resets the timer

- **GIVEN** a Component.Set is sent at t=20s
- **WHEN** the clock advances to t=45s with no further activity
- **THEN** no NoOp has been emitted yet (the next NoOp is scheduled for t=50s, 30s after the most recent outbound)

