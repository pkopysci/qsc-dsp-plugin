# C2NIoRelayDevice

**Namespace:** `gcu_hardware_service.EndpointDevices`

**Inherits:** [`BaseDevice`](BaseDevice.md)

**Implements:** [`IEndpointDevice`](IEndpointDevice.md), [`IRelayDevice`](IRelayDevice.md), [`ICrestronDevice`](ICrestronDevice.md), `IDisposable`

Crestron C2N-IO relay controller connected via Cresnet.

---

## Table of Contents

**Constructors**
- [C2NIoRelayDevice()](#c2niorelaydevice-1)

**Events**
- [RelayChanged](#relaychanged)

**Properties**
- [IsRegistered](#isregistered)
- [SupportsRelays](#supportsrelays)
- [SupportsRs232](#supportsrs232)

**Methods**
- [SetControlSystem(CrestronControlSystem controlSystem)](#setcontrolsystemcrestroncontrolsystem-controlsystem)
- [Initialize(Endpoint endpointData)](#initializeendpoint-endpointdata)
- [Register()](#register)
- [GetCurrentRelayState(int index)](#getcurrentrelaystateint-index)
- [PulseRelay(int index, int timeMs)](#pulserelayint-index-int-timems)
- [LatchRelayClosed(int index)](#latchrelayclosedint-index)
- [LatchRelayOpen(int index)](#latchrelayopenint-index)
- [Dispose()](#dispose)

---

## Constructors

### C2NIoRelayDevice()

```csharp
public C2NIoRelayDevice()
```

Creates a new instance of `C2NIoRelayDevice`. Sets `Manufacturer` to `"Crestron"` and `Model` to `"C2N-IO"`.

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

`true` if the device has been registered with the control system; otherwise `false`.

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

Provides the underlying Crestron control system instance used to register the C2N-IO device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `controlSystem` | `CrestronControlSystem` | The Crestron processor control system reference. |

---

### Initialize(Endpoint endpointData)

```csharp
public void Initialize(Endpoint endpointData)
```

Initialize the C2N-IO device using the port number from the endpoint configuration. Must be called after `SetControlSystem()`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `endpointData` | `Endpoint` | Data object containing the Cresnet port number and device ID. |

---

### Register()

```csharp
public void Register()
```

Register the C2N-IO device with the Crestron control system.

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
