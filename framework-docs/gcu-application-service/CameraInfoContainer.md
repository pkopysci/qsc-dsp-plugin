# CameraInfoContainer

**Namespace:** `gcu_application_service.CameraControl`

**Inherits:** [`InfoContainer`](InfoContainer.md)

Data object representing a single controllable PTZ camera device.

---

## Table of Contents

**Constructors**
- [CameraInfoContainer(...)](#camerainfocontainer-1)

**Properties**
- [Presets](#presets)
- [SupportsSavingPresets](#supportssavingpresets)
- [SupportsZoom](#supportszoom)
- [SupportsPanTilt](#supportspantilt)
- [PowerState](#powerstate)
- [SupportsPowerControl](#supportspowercontrol)

---

## Constructors

### CameraInfoContainer(...)

```csharp
public CameraInfoContainer(string id, string label, string icon, List<string> tags, bool isOnline)
```

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the device. Used for internal referencing. |
| `label` | `string` | The user-friendly name of the device. |
| `icon` | `string` | The image tag used for referencing the UI icon. |
| `tags` | `List<string>` | A collection of custom tags used by the subscribed service. |
| `isOnline` | `bool` | `true` = device is currently connected; `false` = device offline. |

---

## Properties

### Presets

```csharp
public List<InfoContainer> Presets { get; init; }
```

Collection of user-selectable presets for this camera.

---

### SupportsSavingPresets

```csharp
public bool SupportsSavingPresets { get; init; }
```

`true` if preset states can be saved as well as recalled; `false` if save is not supported.

---

### SupportsZoom

```csharp
public bool SupportsZoom { get; init; }
```

`true` if the device implements zoom in/out controls; `false` if zoom is not supported.

---

### SupportsPanTilt

```csharp
public bool SupportsPanTilt { get; init; }
```

`true` if the device implements pan/tilt controls; `false` if not supported.

---

### PowerState

```csharp
public bool PowerState { get; set; }
```

`true` if the device power is on; `false` if off.

---

### SupportsPowerControl

```csharp
public bool SupportsPowerControl { get; set; }
```

`true` if the device supports powering on/off; `false` if power control is not supported.
