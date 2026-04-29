# IZoomDevice

**Namespace:** `gcu_hardware_service.CameraDevices`

Minimum required methods for a device that supports zoom controls.

---

## Table of Contents

**Methods**
- [SetZoom(int speed)](#setzoomint-speed)

---

## Methods

### SetZoom(int speed)

```csharp
void SetZoom(int speed)
```

Zoom in (telephoto) or out (wide angle). Sending `0` (zero) will stop the zoom.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `speed` | `int` | Negative values for zoom wide (out), positive values for zoom telephoto (in). |
