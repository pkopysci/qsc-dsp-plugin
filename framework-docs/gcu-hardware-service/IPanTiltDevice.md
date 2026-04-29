# IPanTiltDevice

**Namespace:** `gcu_hardware_service.CameraDevices`

Minimum required methods for a device that supports pan/tilt controls.

---

## Table of Contents

**Methods**
- [SetPanTilt(Vector2D direction)](#setpantiltvector2d-direction)

---

## Methods

### SetPanTilt(Vector2D direction)

```csharp
void SetPanTilt(Vector2D direction)
```

Pan/tilt the camera. Sending `Vector2D.Zero` will stop the movement.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `direction` | `Vector2D` | The direction to pan and tilt. May be normalized depending on the device implementation. |
