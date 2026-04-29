# ICameraControlApp

**Namespace:** `gcu_application_service.CameraControl`

Events, properties, and methods required for an application service that supports PTZ camera control.

---

## Table of Contents

**Events**
- [CameraControlConnectionChanged](#cameracontrolconnectionchanged)
- [CameraPowerStateChanged](#camerapowerstatechanged)

**Methods**
- [GetAllCameraDeviceInfo()](#getallcameradeviceinfo)
- [SendCameraPanTilt(string cameraId, Vector2D direction)](#sendcamerapantiltstring-cameraid-vector2d-direction)
- [SendCameraZoom(string cameraId, int speed)](#sendcamerazoomstring-cameraid-int-speed)
- [SendCameraPresetRecall(string cameraId, string presetId)](#sendcamerapresetrecallstring-cameraid-string-presetid)
- [SendCameraPresetSave(string cameraId, string presetId)](#sendcamerapresetsavestring-cameraid-string-presetid)
- [QueryCameraConnectionStatus(string id)](#querycameraconnectionstatusstring-id)
- [QueryCameraPowerStatus(string id)](#querycamerapowerstatusstring-id)
- [SendCameraPowerChange(string id, bool newState)](#sendcamerapowerchangestring-id-bool-newstate)

---

## Events

### CameraControlConnectionChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> CameraControlConnectionChanged
```

Triggered when a camera device reports a change in connection status. The event arg is the ID of the camera that changed.

---

### CameraPowerStateChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> CameraPowerStateChanged
```

Triggered when a camera device reports a change in its power state. The event arg is the ID of the camera that changed.

---

## Methods

### GetAllCameraDeviceInfo()

```csharp
ReadOnlyCollection<CameraInfoContainer> GetAllCameraDeviceInfo()
```

**Returns:** A collection of all controllable cameras in the system.

---

### SendCameraPanTilt(string cameraId, Vector2D direction)

```csharp
void SendCameraPanTilt(string cameraId, Vector2D direction)
```

Send a directional pan/tilt command to the target camera.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `cameraId` | `string` | The ID of the camera to adjust. |
| `direction` | `Vector2D` | The direction to pan/tilt the camera. |

---

### SendCameraZoom(string cameraId, int speed)

```csharp
void SendCameraZoom(string cameraId, int speed)
```

Send a zoom command to the camera. Negative values zoom out (wide); positive values zoom in (telephoto).

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `cameraId` | `string` | The ID of the camera to control. |
| `speed` | `int` | A value indicating the zoom direction. Actual speed depends on the hardware. |

---

### SendCameraPresetRecall(string cameraId, string presetId)

```csharp
void SendCameraPresetRecall(string cameraId, string presetId)
```

Recall a camera position preset.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `cameraId` | `string` | The ID of the camera to control. |
| `presetId` | `string` | The ID of the preset to recall. |

---

### SendCameraPresetSave(string cameraId, string presetId)

```csharp
void SendCameraPresetSave(string cameraId, string presetId)
```

Save the current camera position as the target preset, if saving is supported by the device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `cameraId` | `string` | The ID of the camera to control. |
| `presetId` | `string` | The ID of the preset to save. |

---

### QueryCameraConnectionStatus(string id)

```csharp
bool QueryCameraConnectionStatus(string id)
```

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the camera to query. |

**Returns:** `true` if the device is online; `false` if offline.

---

### QueryCameraPowerStatus(string id)

```csharp
bool QueryCameraPowerStatus(string id)
```

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the camera to query. |

**Returns:** `true` if power is on; `false` if power is off.

---

### SendCameraPowerChange(string id, bool newState)

```csharp
void SendCameraPowerChange(string id, bool newState)
```

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the camera to change. |
| `newState` | `bool` | `true` = set power on; `false` = set power off. |
