# IVideoInputSyncDevice

**Namespace:** `gcu_hardware_service.AvSwitchDevices`

Required events, methods, and properties for any AV switcher that supports video input sync monitoring.

---

## Table of Contents

**Events**
- [VideoInputSyncStateChanged](#videoinputsyncstatechanged)

**Methods**
- [QueryVideoInputSyncState(uint input)](#queryvideoinputsyncstateuint-input)

---

## Events

### VideoInputSyncStateChanged

```csharp
event EventHandler<GenericSingleEventArgs<uint>> VideoInputSyncStateChanged
```

Triggered when an input sync status changes. The event argument contains the input number reporting the change.

---

## Methods

### QueryVideoInputSyncState(uint input)

```csharp
bool QueryVideoInputSyncState(uint input)
```

Query the state of the input sync as last reported by the device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `input` | `uint` | The index of the input to query. |

**Returns:** `true` if the input has sync; `false` otherwise.
