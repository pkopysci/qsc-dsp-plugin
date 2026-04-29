# BasicTcpClient

**Namespace:** `gcu_common_utils.NetComs`

**Implements:** `IDisposable`

Simple TCP/IP client for ethernet communications on Crestron control systems. Wraps the Crestron `TCPClient` class with an event-driven interface and optional auto-reconnect support.

---

## Table of Contents

**Constructors**
- [BasicTcpClient(string hostname, int port, int bufferSize)](#basictcpclientstring-hostname-int-port-int-buffersize)

**Events**
- [ConnectionFailed](#connectionfailed)
- [ClientConnected](#clientconnected)
- [StatusChanged](#statuschanged)
- [RxReceived](#rxreceived)
- [RxBytesReceived](#rxbytesreceived)

**Properties**
- [Hostname](#hostname)
- [RxData](#rxdata)
- [RxBytes](#rxbytes)
- [ClientStatusMessage](#clientstatusmessage)
- [Port](#port)
- [BufferSize](#buffersize)
- [Connected](#connected)
- [EnableReconnect](#enablereconnect)
- [ReconnectTime](#reconnecttime)

**Methods**
- [Connect()](#connect)
- [Disconnect()](#disconnect)
- [Send(string data)](#sendstring-data)
- [Send(byte[] data)](#sendbyte-data)
- [Dispose()](#dispose)

---

## Constructors

### BasicTcpClient(string hostname, int port, int bufferSize)

```csharp
public BasicTcpClient(string hostname = "localhost", int port = 80, int bufferSize = 5000)
```

Initializes a new instance of the `BasicTcpClient` class. This class does not check hostname formatting.

**Parameters**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `hostname` | `string` | `"localhost"` | The IP address or hostname to connect with. |
| `port` | `int` | `80` | The port number used to connect. Valid range: 0–65535. |
| `bufferSize` | `int` | `5000` | The size of the read/write stream buffer. |

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | If `hostname` is null or empty. |
| `ArgumentException` | If `port` is outside the range 0–65535 or if `bufferSize` is less than 0. |

---

## Events

### ConnectionFailed

```csharp
public event EventHandler<GenericSingleEventArgs<SocketStatus>>? ConnectionFailed
```

Triggered each time a connection attempt fails. The event argument contains the `SocketStatus` enum value describing the failure reason.

---

### ClientConnected

```csharp
public event EventHandler? ClientConnected
```

Triggered on a successful connection with the host.

---

### StatusChanged

```csharp
public event EventHandler? StatusChanged
```

Triggered whenever the connection status changes. The current status can be obtained from the `ClientStatusMessage` property.

---

### RxReceived

```csharp
public event EventHandler<GenericSingleEventArgs<string>>? RxReceived
```

Triggered whenever any data is received from the server. The event argument contains the received data as a string. Subscribe to this event if string data is desired rather than raw bytes.

---

### RxBytesReceived

```csharp
public event EventHandler<GenericSingleEventArgs<byte[]>>? RxBytesReceived
```

Triggered whenever any data is received from the server. The event argument contains the raw byte data. Subscribe to this event if byte data is desired rather than string data.

---

## Properties

### Hostname

```csharp
public string Hostname { get; }
```

The hostname or IP address set at object creation.

---

### RxData

```csharp
public string RxData { get; }
```

Gets the last set of data sent by the server as a string.

---

### RxBytes

```csharp
public byte[] RxBytes { get; }
```

Gets the most recent response from the server as an array of bytes.

---

### ClientStatusMessage

```csharp
public SocketStatus ClientStatusMessage { get; }
```

Gets the current connection status as a `SocketStatus` enum value.

---

### Port

```csharp
public int Port { get; }
```

Gets the port number being used for connection.

---

### BufferSize

```csharp
public int BufferSize { get; }
```

Gets the current buffer size used when sending or receiving responses from the server. This is set at object creation.

---

### Connected

```csharp
public bool Connected { get; }
```

Gets the current connection status. `true` = client reports connected; `false` = client reports disconnected.

---

### EnableReconnect

```csharp
public bool EnableReconnect { get; set; }
```

Gets or sets whether the client should automatically attempt a reconnect at the `ReconnectTime` interval after a failed connection.

---

### ReconnectTime

```csharp
public int ReconnectTime { get; set; }
```

Gets or sets the time between reconnect attempts, in milliseconds. Defaults to `30000` (30 seconds).

---

## Methods

### Connect()

```csharp
public void Connect()
```

Attempt to connect to the server. If there is a currently active connection, it will be disconnected first.

---

### Disconnect()

```csharp
public void Disconnect()
```

Disconnect from the server if currently connected. Does nothing if there is no active connection.

---

### Send(string data)

```csharp
public void Send(string data)
```

Send a string of information to the server. Length is limited by the value of `BufferSize`. No action is taken if `data` is null.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `data` | `string` | The string data to send to the server. |

---

### Send(byte[] data)

```csharp
public void Send(byte[] data)
```

Send a command to the server as a byte array. No action is taken if `data` is empty.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `data` | `byte[]` | The byte data to send to the server. |

---

### Dispose()

```csharp
public void Dispose()
```

Releases all resources used by the `BasicTcpClient`. Disconnects from the server and disposes the underlying `TCPClient`.
