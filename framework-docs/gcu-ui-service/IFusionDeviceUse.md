# IFusionDeviceUse

**Namespace:** `gcu_ui_service.Fusion.DeviceUse`

Common methods and properties for tracking device usage via Crestron Fusion. Provides methods to register AV source devices and displays for use tracking, and to start and stop recording sessions that are automatically reported to the Fusion server.

---

## Table of Contents

**Methods**
- [AddDeviceToUseTracking(string id, string label)](#adddevicetousetrackinstring-id-string-label)
- [StartDeviceUse(string id)](#startdeviceusestring-id)
- [StopDeviceUse(string id)](#stopdeviceusestring-id)
- [AddDisplayToUseTracking(string id, string label)](#adddisplaytousetrackinstring-id-string-label)
- [StartDisplayUse(string id)](#startdisplayusestring-id)
- [StopDisplayUse(string id)](#stopdisplayusestring-id)

---

## Methods

### AddDeviceToUseTracking(string id, string label)

```csharp
void AddDeviceToUseTracking(string id, string label)
```

Add a device to the internal collection used for tracking use.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the device to track. Used when starting or stopping an in-use event. |
| `label` | `string` | The user-friendly label of the device. Logged and displayed when reporting use statistics. |

---

### StartDeviceUse(string id)

```csharp
void StartDeviceUse(string id)
```

Start recording use time for the target device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of a device that was added with `AddDeviceToUseTracking()`. |

---

### StopDeviceUse(string id)

```csharp
void StopDeviceUse(string id)
```

Stop recording the use time for the target device and send a usage log to the Fusion server.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of a device that was added with `AddDeviceToUseTracking()`. |

---

### AddDisplayToUseTracking(string id, string label)

```csharp
void AddDisplayToUseTracking(string id, string label)
```

Add a display to the internal collection used for tracking use statistics.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the display to track. Used when starting or stopping use tracking. |
| `label` | `string` | The user-friendly name of the display. Used in the usage report. |

---

### StartDisplayUse(string id)

```csharp
void StartDisplayUse(string id)
```

Start recording the use time for the target display.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the display to start tracking. |

---

### StopDisplayUse(string id)

```csharp
void StopDisplayUse(string id)
```

Stop recording the use time for the target display and send the data to the Fusion server.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the display to stop and send a report for. |
