# IDsp

**Namespace:** `gcu_hardware_service.AudioDevices`

**Implements:** [`IAudioControl`](IAudioControl.md), `IDisposable`

Common properties and methods for controlling typical DSP devices. All DSP device plugins must implement this interface. Extends `IAudioControl` with a device initialization method.

---

## Table of Contents

**Methods**
- [Initialize(...)](#initialize)

---

## Methods

### Initialize(...)

```csharp
void Initialize(
    string hostId,
    int coreId,
    string hostname,
    int port,
    string username,
    string password)
```

Sets internal object configuration based on the supplied data. Does not connect to the device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `hostId` | `string` | The unique ID of the DSP being controlled. |
| `coreId` | `int` | The device number used by the hardware when sending or receiving data. Can be set to `0` if unused. |
| `hostname` | `string` | The hostname or IP address used to connect to the hardware. |
| `port` | `int` | The TCP port number used to connect to the hardware. |
| `username` | `string` | The authentication username used when connecting. |
| `password` | `string` | The authentication password used when connecting. |

## Related Types

- [IAudioZoneEnabler](IAudioZoneEnabler.md) — Optional interface for DSPs that support audio zone routing.
- [IDspLogicTriggerSupport](IDspLogicTriggerSupport.md) — Optional interface for DSPs that support logic trigger control.
