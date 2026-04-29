# IRelayDevice

**Namespace:** `gcu_hardware_service.EndpointDevices`

**Implements:** [`IBaseDevice`](IBaseDevice.md)

Common properties and methods for basic relay device control.

---

## Table of Contents

**Events**
- [RelayChanged](#relaychanged)

**Methods**
- [GetCurrentRelayState(int index)](#getcurrentrelaystateint-index)
- [PulseRelay(int index, int timeMs)](#pulserelayint-index-int-timems)
- [LatchRelayClosed(int index)](#latchrelayclosedint-index)
- [LatchRelayOpen(int index)](#latchrelayopenint-index)

---

## Events

### RelayChanged

```csharp
event EventHandler<GenericDualEventArgs<string, int>> RelayChanged
```

Triggered whenever the state of a relay changes (open → closed or closed → open). `Arg1` is the device ID; `Arg2` is the 1-based relay ID that changed.

---

## Methods

### GetCurrentRelayState(int index)

```csharp
bool? GetCurrentRelayState(int index)
```

Gets a value indicating whether the relay is closed or open.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `index` | `int` | The 0-based index of the relay to query. |

**Returns:** `true` if the relay is closed; `false` if open; `null` if the state cannot be determined.

---

### PulseRelay(int index, int timeMs)

```csharp
void PulseRelay(int index, int timeMs)
```

Close the relay for the specified amount of time, then reopen it.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `index` | `int` | The 0-based index of the relay to control. |
| `timeMs` | `int` | The amount of time in milliseconds that the relay should remain closed. |

---

### LatchRelayClosed(int index)

```csharp
void LatchRelayClosed(int index)
```

Sets the relay closed until `LatchRelayOpen()` or `PulseRelay()` is called.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `index` | `int` | The 0-based index of the relay to control. |

---

### LatchRelayOpen(int index)

```csharp
void LatchRelayOpen(int index)
```

Sets the relay open until `LatchRelayClosed()` or `PulseRelay()` is called.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `index` | `int` | The 0-based index of the relay to control. |
