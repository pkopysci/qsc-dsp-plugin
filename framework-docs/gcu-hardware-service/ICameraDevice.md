# ICameraDevice

**Namespace:** `gcu_hardware_service.CameraDevices`

**Implements:** [`IBaseDevice`](IBaseDevice.md)

Properties, events, and methods required for any PTZ camera device plugin.

---

## Table of Contents

**Methods**
- [Initialize(...)](#initialize)

---

## Methods

### Initialize(...)

```csharp
void Initialize(
    string hostname,
    int port,
    string id,
    string label,
    string username,
    string password)
```

Configure the camera's connection parameters. Does not establish a connection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `hostname` | `string` | The IP address or hostname used to connect to the hardware. |
| `port` | `int` | The port number used to connect to the hardware. |
| `id` | `string` | A unique ID used to reference this device. |
| `label` | `string` | A human-friendly name of this device. |
| `username` | `string` | The authentication username used when connecting. |
| `password` | `string` | The authentication password used when connecting. |

## Related Types

- [IPanTiltDevice](IPanTiltDevice.md) — Optional interface for cameras that support pan/tilt controls.
- [IZoomDevice](IZoomDevice.md) — Optional interface for cameras that support zoom controls.
- [IPresetDevice](IPresetDevice.md) — Optional interface for cameras that support preset recall and save.
- [CameraPreset](CameraPreset.md) — Data type for a single camera preset.
