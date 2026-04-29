# CrestronComSpecHelper

**Namespace:** `gcu_common_utils.SerialComs`

Static helper class for converting generic comm spec string/integer values from configuration into the Crestron `ComPort` enum types. Writes warnings or errors to the logging system when unrecognized values are encountered and falls back to defaults.

---

## Table of Contents

**Methods**
- [GetBaudRate(int baudRate)](#getbaudrateint-baudrate)
- [GetDataBits(int data)](#getdatabitsint-data)
- [GetStopBits(int data)](#getstopbitsint-data)
- [GetHwHandshake(string data)](#gethwhandshakestring-data)
- [GetSwHandshake(string data)](#getswhandshakestring-data)
- [GetParity(string data)](#getparitystring-data)
- [GetProtocol(string data)](#getprotocolstring-data)

---

## Methods

### GetBaudRate(int baudRate)

```csharp
public static ComPort.eComBaudRates GetBaudRate(int baudRate)
```

Convert a configuration baud rate argument to a Crestron `ComPort` baud rate enum value. Writes a warning to the logging system if an unrecognized value is encountered.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `baudRate` | `int` | The config-defined baud rate to convert. Supported values: `300`, `600`, `1200`, `1800`, `2400`, `3600`, `4800`, `7200`, `9600`, `14400`, `19200`, `28800`, `38400`, `57600`, `115200`. |

**Returns:** The `ComPort.eComBaudRates` equivalent. Defaults to `9600` if the value cannot be parsed.

---

### GetDataBits(int data)

```csharp
public static ComPort.eComDataBits GetDataBits(int data)
```

Convert a configuration data bits number to a Crestron `ComPort` data bits enum value. Writes a warning to the logging system if an unrecognized argument is given.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `data` | `int` | The data bits number set in the configuration. Supported values: `7`, `8`. |

**Returns:** The `ComPort.eComDataBits` equivalent. Defaults to `8` if the value cannot be parsed.

---

### GetStopBits(int data)

```csharp
public static ComPort.eComStopBits GetStopBits(int data)
```

Convert a configuration stop bits number to a Crestron `ComPort` stop bits enum value. Writes a warning to the logging system if an unrecognized argument is given.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `data` | `int` | The stop bits number set in the configuration. Supported values: `1`, `2`. |

**Returns:** The `ComPort.eComStopBits` equivalent. Defaults to `1` if the value cannot be parsed.

---

### GetHwHandshake(string data)

```csharp
public static ComPort.eComHardwareHandshakeType GetHwHandshake(string data)
```

Convert a configuration hardware handshake argument to a Crestron `ComPort` hardware handshake enum value. Writes a warning to the logging system if an unrecognized argument is given.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `data` | `string` | The hardware handshake argument set in the configuration. Supported values (case-insensitive): `NONE`, `RTS`, `CTS`, `CTS/RTS`. |

**Returns:** The `ComPort.eComHardwareHandshakeType` equivalent. Defaults to `None` if the value cannot be parsed.

---

### GetSwHandshake(string data)

```csharp
public static ComPort.eComSoftwareHandshakeType GetSwHandshake(string data)
```

Convert a configuration software handshake argument to a Crestron `ComPort` software handshake enum value. Writes an error to the logging system if an unrecognized argument is given.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `data` | `string` | The software handshake argument set in the configuration. Supported values (case-insensitive): `NONE`, `XON`, `XONT`, `XONR`. |

**Returns:** The `ComPort.eComSoftwareHandshakeType` equivalent. Defaults to `None` if the value cannot be parsed.

---

### GetParity(string data)

```csharp
public static ComPort.eComParityType GetParity(string data)
```

Convert a configuration parity argument to a Crestron `ComPort` parity enum value. Writes an error to the logging system if an unrecognized argument is given.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `data` | `string` | The parity argument set in the configuration. Supported values (case-insensitive): `NONE`, `EVEN`, `ODD`. |

**Returns:** The `ComPort.eComParityType` equivalent. Defaults to `None` if the value cannot be parsed.

---

### GetProtocol(string data)

```csharp
public static ComPort.eComProtocolType GetProtocol(string data)
```

Convert a configuration serial protocol argument to a Crestron `ComPort` protocol enum value. Writes an error to the logging system if an unrecognized argument is given.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `data` | `string` | The serial protocol argument set in the configuration. Supported values (case-insensitive): `RS232`, `RS422`, `RS485`. |

**Returns:** The `ComPort.eComProtocolType` equivalent. Defaults to `RS232` if the value cannot be parsed.
