# ComSpec

**Namespace:** `gcu_domain_service.Data.ConnectionData`

Serial communication specification for a device connection. Defines the RS-232/RS-422/RS-485 protocol parameters used when `Connection.Transport` is set to `serial`. Values in this object are passed to [`CrestronComSpecHelper`](../gcu-common-utils/CrestronComSpecHelper.md) to produce the Crestron `ComPort` enum values.

---

## Table of Contents

**Properties**
- [Protocol](#protocol)
- [BaudRate](#baudrate)
- [DataBits](#databits)
- [StopBits](#stopbits)
- [HwHandshake](#hwhandshake)
- [SwHandshake](#swhandshake)
- [Parity](#parity)

---

## Properties

### Protocol

```csharp
public string Protocol { get; set; }
```

**Type:** `string`

The serial protocol type. Supported values: `RS232`, `RS422`, `RS485`. Defaults to `string.Empty`.

---

### BaudRate

```csharp
public int BaudRate { get; set; }
```

**Type:** `int`

The baud rate for serial communication (e.g., `9600`, `115200`). Defaults to `0`.

---

### DataBits

```csharp
public int DataBits { get; set; }
```

**Type:** `int`

The number of data bits per frame (typically `7` or `8`). Defaults to `0`.

---

### StopBits

```csharp
public int StopBits { get; set; }
```

**Type:** `int`

The number of stop bits per frame (typically `1` or `2`). Defaults to `0`.

---

### HwHandshake

```csharp
public string HwHandshake { get; set; }
```

**Type:** `string`

Hardware handshake setting. Supported values: `NONE`, `RTS`, `CTS`, `CTS/RTS`. Defaults to `string.Empty`.

---

### SwHandshake

```csharp
public string SwHandshake { get; set; }
```

**Type:** `string`

Software handshake setting. Supported values: `NONE`, `XON`, `XONT`, `XONR`. Defaults to `string.Empty`.

---

### Parity

```csharp
public string Parity { get; set; }
```

**Type:** `string`

Parity setting. Supported values: `NONE`, `EVEN`, `ODD`. Defaults to `string.Empty`.
