# ISerialPort

**Namespace:** `gcu_common_utils.SerialComs`

Interface defining the minimum events, properties, and methods required for any communication implementation that uses serial (COM port) control.

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
event EventHandler<GenericSingleEventArgs<string>>? StringDataReceived
```

Event triggered whenever any data is received from the serial comm port. The event argument contains the string representation of the data received.

---

### ByteDataReceived

```csharp
event EventHandler<GenericSingleEventArgs<byte[]>>? ByteDataReceived
```

Event triggered whenever any data is received from the serial comm port. The event argument contains the byte array representation of the data received.

---

### EnableStatusChanged

```csharp
event EventHandler? EnableStatusChanged
```

Event triggered whenever the comm port enable status changes. Typically triggered by a call to `Enable()` or `Disable()`.

---

## Properties

### IsEnabled

```csharp
bool IsEnabled { get; }
```

`true` = the comm port is enabled for sending and receiving data; `false` = disabled.

---

### Id

```csharp
string Id { get; }
```

A unique ID used to reference this object.

---

## Methods

### SetComSpec(...)

```csharp
void SetComSpec(
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

Set the configuration specifications for the comm port.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `protocol` | `string` | The serial protocol (e.g., `RS232`, `RS422`, `RS485`). |
| `baudRate` | `int` | The baud rate for communication (e.g., `9600`, `115200`). |
| `dataBits` | `int` | The number of data bits (e.g., `7` or `8`). |
| `stopBits` | `int` | The number of stop bits (e.g., `1` or `2`). |
| `hardwareHandshake` | `string` | Hardware handshake setting (e.g., `NONE`, `RTS`, `CTS`, `CTS/RTS`). |
| `softwareHandshake` | `string` | Software handshake setting (e.g., `NONE`, `XON`, `XONT`, `XONR`). |
| `parity` | `string` | Parity setting (e.g., `NONE`, `EVEN`, `ODD`). |
| `reportCtsChanges` | `bool` | Whether to report CTS (Clear To Send) status changes. |

---

### Enable()

```csharp
void Enable()
```

Register the comm port and begin monitoring for device responses.

---

### Disable()

```csharp
void Disable()
```

Unregister the comm port and stop monitoring for device responses.

---

### Send(byte[] data)

```csharp
void Send(byte[] data)
```

Send a command to the device as a byte array. Does nothing if the comm port has not been enabled.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `data` | `byte[]` | The byte data to send. |

---

### Send(string data)

```csharp
void Send(string data)
```

Send a command to the device as a string. Does nothing if the comm port has not been enabled.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `data` | `string` | The string data to send. |
