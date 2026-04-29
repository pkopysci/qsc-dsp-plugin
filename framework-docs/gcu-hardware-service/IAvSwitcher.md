# IAvSwitcher

**Namespace:** `gcu_hardware_service.AvSwitchDevices`

**Implements:** [`IBaseDevice`](IBaseDevice.md), [`IVideoRoutable`](IVideoRoutable.md)

Properties and methods common to all devices that are capable of audio and video routing.

---

## Table of Contents

**Methods**
- [Initialize(...)](#initialize)

---

## Methods

### Initialize(...)

```csharp
void Initialize(
    string hostName,
    int port,
    string id,
    string label,
    int numInputs,
    int numOutputs)
```

Initialize the device with the given data. Does not connect to the device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `hostName` | `string` | The IP address or hostname used to connect. |
| `port` | `int` | The port number used to connect. |
| `id` | `string` | The unique ID of the device. |
| `label` | `string` | The user-friendly name of the device. |
| `numInputs` | `int` | Number of inputs supported. |
| `numOutputs` | `int` | Number of outputs supported. |

## Related Types

- [IVideoInputSyncDevice](IVideoInputSyncDevice.md) — Optional interface for switchers that support video input sync monitoring.
- [AvSwitchCommands](AvSwitchCommands.md) — Enum of command types used by AV switchers.
