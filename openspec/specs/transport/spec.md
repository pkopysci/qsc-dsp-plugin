# transport Specification

## Purpose
TBD - created by archiving change add-qrc-client-and-connection. Update Purpose after archive.
## Requirements
### Requirement: Transport abstraction over BasicTcpClient

The plugin SHALL define an `IConnectionTransport` interface that exposes byte-oriented `Send(byte[])`, a `RxReceived` event, a `ConnectionFailed` event, an `IsConnected` property, and `Connect()`/`Disconnect()` methods. Production code SHALL provide one implementation, `BasicTcpClientTransport`, that wraps `gcu_common_utils.NetComs.BasicTcpClient` (the only sanctioned TCP client per README §4).

#### Scenario: Production transport is constructed against the framework's BasicTcpClient

- **WHEN** the plugin is initialized with a hostname and port
- **THEN** a `BasicTcpClientTransport` is constructed wrapping a `BasicTcpClient(hostname, port, bufferSize)`
- **AND** the transport's events are wired to the underlying `BasicTcpClient` events (ClientConnected, RxBytesReceived, ConnectionFailed)

### Requirement: Transport reports connection failures, never throws across boundaries

The transport's `Connect()` method MUST NOT throw an exception across the plugin boundary on failure. Failures SHALL be reported by raising the `ConnectionFailed` event and returning normally; the connection manager observes the event and drives the state machine.

#### Scenario: Connect to unreachable host

- **GIVEN** a transport configured to connect to an unreachable host
- **WHEN** `Connect()` is called
- **THEN** the call returns without throwing
- **AND** within a bounded time the `ConnectionFailed` event fires

### Requirement: Transport is fully Disposable and idempotent

`IConnectionTransport` SHALL extend `IDisposable`. Calling `Dispose()` more than once MUST NOT throw. After `Dispose()`, all event handlers SHALL be detached and underlying socket resources SHALL be released.

#### Scenario: Double-dispose is safe

- **GIVEN** a transport that has been disposed
- **WHEN** `Dispose()` is called a second time
- **THEN** the second call returns without throwing

### Requirement: A test-only RawTcpTransport exists in TestSupport

The TestSupport project SHALL provide a `RawTcpTransport` that implements `IConnectionTransport` against `System.Net.Sockets.TcpClient` directly. This avoids invoking the `BasicTcpClient` stub during tests (the stub throws `NotImplementedException` by spec).

#### Scenario: Integration test connects to FakeQrcServer via RawTcpTransport

- **GIVEN** a `FakeQrcServer` listening on a free local port
- **WHEN** an integration test constructs a `RawTcpTransport` for that endpoint
- **AND** calls `Connect()`
- **THEN** the transport's `RxReceived` event fires when the FakeQrcServer sends an `EngineStatus` message
- **AND** no `NotImplementedException` is thrown

