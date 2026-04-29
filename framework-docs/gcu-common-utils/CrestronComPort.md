# CrestronComPort

**Namespace:** `gcu_common_utils.SerialComs`

**Implements:** [`ISerialPort`](ISerialPort.md)

Abstraction wrapper class for the Crestron `ComPort`. Provides a consistent interface for serial communication on Crestron control systems, including automatic registration/unregistration and event-driven data reception.

**Constructor**

```csharp
public CrestronComPort(string id, ComPort comPort)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | `string` | A unique identifier for this port instance. |
| `comPort` | `ComPort` | The underlying Crestron `ComPort` to wrap. |

---

## Table of Contents

**Events**
- [StringDataReceived](#stringdatareceived)
- [ByteDataReceived](#bytedatareceived)
- [EnableStatusChanged](#enablestatuschanged)

**Properties**
- [IsEnabled](#isenabled)
- [Id](#id)

**Methods**
- [SetComSpec(...)](#setcomspec)
- [Enable()](#enable)
- [Disable()](#disable)
- [Send(byte[] data)](#sendbyte-data)
- [Send(string data)](#sendstring-data)

---

## Events

### StringDataReceived

```csharp
public event EventHandler<GenericSingleEventArgs<string>>? StringDataReceived
```

Triggered whenever any data is received from the serial comm port. The event argument contains the string representation of the received data.

---

### ByteDataReceived

```csharp
public event EventHandler<GenericSingleEventArgs<byte[]>>? ByteDataReceived
```

Triggered whenever any data is received from the serial comm port. The event argument contains the byte array representation of the received data.

---

### EnableStatusChanged

```csharp
public event EventHandler? EnableStatusChanged
```

Triggered whenever the comm port enable status changes. Raised by calls to `Enable()` or `Disable()`.

---

## Properties

### IsEnabled

```csharp
public bool IsEnabled { get; }
```

`true` = the comm port is enabled and the underlying Crestron `ComPort` is registered; `false` = disabled or unregistered.

---

### Id

```csharp
public string Id { get; }
```

The unique identifier for this port instance, as provided at construction.

---

## Methods

### SetComSpec(...)

```csharp
public void SetComSpec(
    string protocol,
    int baudRate,
    int dataBits,
    int stopBits,
    string hardwareHandshake,
    string softwareHandshake,
    string parity,
    bool reportCtsChanges
)
```

Set the configuration specifications for the comm port. Settings are applied to the underlying Crestron `ComPort` when `Enable()` is called. Uses [`CrestronComSpecHelper`](CrestronComSpecHelper.md) to convert string/integer config values to the appropriate Crestron enums.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `protocol` | `string` | The serial protocol (e.g., `RS232`, `RS422`, `RS485`). |
| `baudRate` | `int` | The baud rate for communication (e.g., `9600`, `115200`). |
| `dataBits` | `int` | The number of data bits (`7` or `8`). |
| `stopBits` | `int` | The number of stop bits (`1` or `2`). |
| `hardwareHandshake` | `string` | Hardware handshake setting (`NONE`, `RTS`, `CTS`, `CTS/RTS`). |
| `softwareHandshake` | `string` | Software handshake setting (`NONE`, `XON`, `XONT`, `XONR`). |
| `parity` | `string` | Parity setting (`NONE`, `EVEN`, `ODD`). |
| `reportCtsChanges` | `bool` | Whether to report CTS (Clear To Send) status changes. |

---

### Enable()

```csharp
public void Enable()
```

Registers the comm port with the Crestron system and applies the com spec configuration. Begins monitoring for device responses. Logs an error if registration fails. Raises `EnableStatusChanged` on success. Does nothing if already enabled.

---

### Disable()

```csharp
public void Disable()
```

Unregisters the comm port and stops monitoring for device responses. Raises `EnableStatusChanged`. Does nothing if not currently enabled.

---

### Send(byte[] data)

```csharp
public void Send(byte[] data)
```

Send a command to the device as a byte array. Does nothing if the comm port is not enabled. The byte data is converted to a string using ISO-8859-1 encoding before sending.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `data` | `byte[]` | The byte data to send. |

---

### Send(string data)

```csharp
public void Send(string data)
```

Send a command to the device as a string. Does nothing if the comm port is not enabled.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `data` | `string` | The string data to send. |
