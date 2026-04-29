# CenIoRy401RelayDevice

**Namespace:** `gcu_hardware_service.EndpointDevices`

**Inherits:** [`BaseDevice`](BaseDevice.md)

**Implements:** [`IEndpointDevice`](IEndpointDevice.md), [`IRelayDevice`](IRelayDevice.md), [`ICrestronDevice`](ICrestronDevice.md), `IDisposable`

Crestron CEN-IO-RY-401 relay controller connected via Ethernet.

---

## Table of Contents

**Constructors**
- [CenIoRy401RelayDevice()](#cenio-ry401relaydevice-1)

**Events**
- [RelayChanged](#relaychanged)

**Properties**
- [IsRegistered](#isregistered)
- [SupportsRelays](#supportsrelays)
- [SupportsRs232](#supportsrs232)

**Methods**
- [SetControlSystem(CrestronControlSystem controlSystem)](#setcontrolsystemcrestroncontrolsystem-controlsystem)
- [Initialize(Endpoint configData)](#initializeendpoint-configdata)
- [Register()](#register)
- [GetCurrentRelayState(int index)](#getcurrentrelaystateint-index)
- [PulseRelay(int index, int timeMs)](#pulserelayint-index-int-timems)
- [LatchRelayClosed(int index)](#latchrelayclosedint-index)
- [LatchRelayOpen(int index)](#latchrelayopenint-index)
- [Dispose()](#dispose)

---

## Constructors

### CenIoRy401RelayDevice()

```csharp
public CenIoRy401RelayDevice()
```

Creates a new instance of `CenIoRy401RelayDevice`. Sets `Manufacturer` to `"Crestron"` and `Model` to `"CEN-IO-RY104"`.

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

`true` if the device has been registered; otherwise `false`.

---

### SupportsRelays

```csharp
public bool SupportsRelays { get; }
```

Always `true` for this device.

---

### SupportsRs232

```csharp
public bool SupportsRs232 { get; }
```

Always `false` for this device.

---

## Methods

### SetControlSystem(CrestronControlSystem controlSystem)

```csharp
public void SetControlSystem(CrestronControlSystem controlSystem)
```

Provides the underlying Crestron control system instance used to register the CEN-IO-RY-401 device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `controlSystem` | `CrestronControlSystem` | The Crestron processor control system reference. |

---

### Initialize(Endpoint configData)

```csharp
public void Initialize(Endpoint configData)
```

Initialize the CEN-IO-RY-401 device using the port number from the configuration. Must be called after `SetControlSystem()`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `configData` | `Endpoint` | Data object containing the IP ID/port number and device ID. |

---

### Register()

```csharp
public void Register()
```

Register the CEN-IO-RY-401 device with the Crestron control system.

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

**Returns:** `true` if closed; `false` if open or device is not registered.

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

### Dispose()

```csharp
public void Dispose()
```

Releases all relay resources. Opens all relays, unregisters, and disposes the underlying device before cleanup.
