# TcpClientWrapper

**Namespace:** `gcu_common_utils.NetComs`

**Implements:** `IDisposable`

An asynchronous wrapper that manages an underlying .NET `TcpClient` object. Designed for use in standard .NET environments (not Crestron-specific). Communicates connection and data events through callback delegates configured at initialization.

---

## Table of Contents

**Properties**
- [ConnectionTimeout](#connectiontimeout)
- [IpAddress](#ipaddress)
- [Port](#port)
- [IsConnected](#isconnected)
- [OnConnectedCallback](#onconnectedcallback)
- [OnDisconnectedCallback](#ondisconnectedcallback)
- [OnDataReceivedCallback](#ondatareceivedcallback)
- [OnConnectionFailedCallback](#onconnectionfailedcallback)

**Methods**
- [ConnectAsync()](#connectasync)
- [Disconnect()](#disconnect)
- [SendAsync(byte[] data, CancellationToken cancellationToken)](#sendasyncbyte-data-cancellationtoken-cancellationtoken)
- [Dispose()](#dispose)

---

## Properties

### ConnectionTimeout

```csharp
public TimeSpan ConnectionTimeout { get; set; }
```

Gets or sets the amount of time to wait for a connection before reporting a failure to connect. Defaults to 30 seconds.

---

### IpAddress

```csharp
public string IpAddress { get; init; }
```

The IP address used to connect to the remote host. Defaults to `127.0.0.1`.

---

### Port

```csharp
public int Port { get; init; }
```

The port number used to connect to the remote host. Defaults to `80`.

---

### IsConnected

```csharp
public bool IsConnected { get; }
```

`true` = the client is currently connected to a remote host; `false` = disconnected.

---

### OnConnectedCallback

```csharp
public Func<TcpClientWrapper, Task>? OnConnectedCallback { get; init; }
```

Callback method triggered when the client successfully connects to a remote host.

---

### OnDisconnectedCallback

```csharp
public Func<TcpClientWrapper, bool, Task>? OnDisconnectedCallback { get; init; }
```

Callback method triggered when the client disconnects from the remote host for any reason. The `bool` parameter indicates if the disconnect was caused by the remote host (`true`) or by a local call to `Disconnect()` (`false`).

---

### OnDataReceivedCallback

```csharp
public Func<TcpClientWrapper, byte[], Task>? OnDataReceivedCallback { get; init; }
```

Callback method invoked when any data is received from the remote host. The `byte[]` parameter contains the received data.

**Remarks:** This will block incoming data until this callback completes.

---

### OnConnectionFailedCallback

```csharp
public Action<TcpClientWrapper, string>? OnConnectionFailedCallback { get; init; }
```

Callback method triggered each time the client fails to connect within the time set by `ConnectionTimeout`. The `string` parameter contains an error message describing the failure.

---

## Methods

### ConnectAsync()

```csharp
public async Task ConnectAsync()
```

Attempts an asynchronous connection to the server defined by `IpAddress`:`Port`. If a connection is not established within the time defined by `ConnectionTimeout`, the underlying client is closed and `OnConnectionFailedCallback` is invoked.

---

### Disconnect()

```csharp
public void Disconnect()
```

Closes the connection with the remote server and releases the internal `TcpClient` resources.

---

### SendAsync(byte[] data, CancellationToken cancellationToken)

```csharp
public async Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
```

Attempts to send data to the remote server. Does nothing if there is no active connection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `data` | `byte[]` | The byte data to send to the server. Cannot be null. |
| `cancellationToken` | `CancellationToken` | A cancellation token forwarded to the socket stream. |

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | If `data` is null. |

---

### Dispose()

```csharp
public void Dispose()
```

Closes the existing connection and releases all `TcpClient` resources.
