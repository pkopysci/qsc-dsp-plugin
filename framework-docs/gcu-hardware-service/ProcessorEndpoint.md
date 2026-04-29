# ProcessorEndpoint

**Namespace:** `gcu_hardware_service.EndpointDevices`

**Inherits:** [`BaseDevice`](BaseDevice.md)

**Implements:** [`IEndpointDevice`](IEndpointDevice.md), [`IRelayDevice`](IRelayDevice.md), [`ISerialEndpoint`](ISerialEndpoint.md), [`IIrEndpoint`](IIrEndpoint.md), [`ICrestronDevice`](ICrestronDevice.md)

Endpoint control wrapper for accessing relay, RS-232, and IR controls on a Crestron control processor.

---

## Table of Contents

**Constructors**
- [ProcessorEndpoint()](#processorendpoint-1)

**Events**
- [RelayChanged](#relaychanged)

**Properties**
- [IsRegistered](#isregistered)
- [SupportsRelays](#supportsrelays)
- [SupportsRs232](#supportsrs232)

**Methods**
- [Initialize(Endpoint configData)](#initializeendpoint-configdata)
- [SetControlSystem(CrestronControlSystem controlSystem)](#setcontrolsystemcrestroncontrolsystem-controlsystem)
- [Register()](#register)
- [GetCurrentRelayState(int index)](#getcurrentrelaystateint-index)
- [LatchRelayClosed(int index)](#latchrelayclosedint-index)
- [LatchRelayOpen(int index)](#latchrelayopenint-index)
- [PulseRelay(int index, int timeMs)](#pulserelayint-index-int-timems)
- [GetComPort(int port)](#getcomportint-port)
- [GetIrPort(int port)](#getirportint-port)

---

## Constructors

### ProcessorEndpoint()

```csharp
public ProcessorEndpoint()
```

Instantiates a new instance of `ProcessorEndpoint`. Sets `Manufacturer` to `"Crestron"` and `Model` to `"Processor"`.

---

## Events

### RelayChanged

```csharp
public event EventHandler<GenericDualEventArgs<string, int>>? RelayChanged
```

Triggered whenever a relay state changes. `Arg1` is the device ID; `Arg2` is the 1-based relay ID that changed.

---

## Properties

### IsRegistered

```csharp
public bool IsRegistered { get; }
```

`true` if the processor endpoint has been successfully registered; otherwise `false`.

---

### SupportsRelays

```csharp
public bool SupportsRelays { get; }
```

`true` if the underlying control system supports relay ports; otherwise `false`.

---

### SupportsRs232

```csharp
public bool SupportsRs232 { get; }
```

`true` if the underlying control system supports com ports; otherwise `false`.

---

## Methods

### Initialize(Endpoint configData)

```csharp
public void Initialize(Endpoint configData)
```

Initialize internal control objects from the given configuration. Sets the device `Id` from config data.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `configData` | `Endpoint` | Data object containing connection and port information. |

---

### SetControlSystem(CrestronControlSystem controlSystem)

```csharp
public void SetControlSystem(CrestronControlSystem controlSystem)
```

Provides the underlying Crestron control system instance used to access hardware ports.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `controlSystem` | `CrestronControlSystem` | The Crestron processor control system reference. |

---

### Register()

```csharp
public void Register()
```

Register relay and IR ports on the underlying control system. Sets `IsRegistered` to `true` and marks the device online upon success.

---

### GetCurrentRelayState(int index)

```csharp
public bool? GetCurrentRelayState(int index)
```

Gets whether the relay at the specified index is closed.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `index` | `int` | The 0-based index of the relay to query. |

**Returns:** `true` if closed; `false` if open or index is invalid; `null` if state cannot be determined.

---

### LatchRelayClosed(int index)

```csharp
public void LatchRelayClosed(int index)
```

Closes the relay at the specified index until `LatchRelayOpen()` or `PulseRelay()` is called.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `index` | `int` | The 0-based index of the relay to close. |

---

### LatchRelayOpen(int index)

```csharp
public void LatchRelayOpen(int index)
```

Opens the relay at the specified index until `LatchRelayClosed()` or `PulseRelay()` is called.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `index` | `int` | The 0-based index of the relay to open. |

---

### PulseRelay(int index, int timeMs)

```csharp
public void PulseRelay(int index, int timeMs)
```

Close the relay for the specified duration, then reopen it.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `index` | `int` | The 0-based index of the relay to pulse. |
| `timeMs` | `int` | The duration in milliseconds to hold the relay closed. |

---

### GetComPort(int port)

```csharp
public ISerialPort? GetComPort(int port)
```

Get the serial com port at the specified index from the control system.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `port` | `int` | The index of the port to retrieve. |

**Returns:** The `ISerialPort` at the given index, or `null` if the port is unavailable or out of range.

---

### GetIrPort(int port)

```csharp
public IIrPort GetIrPort(int port)
```

Get the IR output port at the specified index from the control system. The port is registered and enabled before being returned.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `port` | `int` | The index of the IR port to retrieve. |

**Returns:** The `IIrPort` at the given index.

**Exceptions**

| Type | Condition |
|------|-----------|
| `ArgumentOutOfRangeException` | Thrown if `port` is outside the range of available IR ports. |
| `ArgumentException` | Thrown if the target IR port does not exist. |
