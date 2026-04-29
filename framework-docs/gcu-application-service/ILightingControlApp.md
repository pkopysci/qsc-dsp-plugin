# ILightingControlApp

**Namespace:** `gcu_application_service.LightingControl`

Required events, methods, and properties for implementing an application service that supports lighting controls.

---

## Table of Contents

**Events**
- [LightingLoadLevelChanged](#lightingloadlevelchanged)
- [LightingSceneChanged](#lightingscenechanged)
- [LightingControlConnectionChanged](#lightingcontrolconnectionchanged)

**Methods**
- [RecallLightingScene(string deviceId, string sceneId)](#recalllightingscenestring-deviceid-string-sceneid)
- [SetLightingLoad(string deviceId, string zoneId, int level)](#setlightingloadstring-deviceid-string-zoneid-int-level)
- [GetActiveScene(string deviceId)](#getactivescenestring-deviceid)
- [GetZoneLoad(string deviceId, string zoneId)](#getzoneloadstring-deviceid-string-zoneid)
- [GetAllLightingDeviceInfo()](#getalllightingdeviceinfo)

---

## Events

### LightingLoadLevelChanged

```csharp
event EventHandler<GenericDualEventArgs<string, string>> LightingLoadLevelChanged
```

Triggered whenever a lighting controller reports that a zone load level has changed. `Arg1` is the controller ID; `Arg2` is the zone ID.

---

### LightingSceneChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> LightingSceneChanged
```

Triggered whenever a lighting controller reports that a new scene has been recalled. The event arg is the ID of the controller that changed.

---

### LightingControlConnectionChanged

```csharp
event EventHandler<GenericDualEventArgs<string, bool>> LightingControlConnectionChanged
```

Triggered when any lighting controller reports an online/offline status change. `Arg1` is the controller ID; `Arg2` is the new connection state.

---

## Methods

### RecallLightingScene(string deviceId, string sceneId)

```csharp
void RecallLightingScene(string deviceId, string sceneId)
```

Request to recall a saved lighting scene on the target controller.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `deviceId` | `string` | The unique ID of the lighting controller to change. |
| `sceneId` | `string` | The unique ID of the scene to recall. |

---

### SetLightingLoad(string deviceId, string zoneId, int level)

```csharp
void SetLightingLoad(string deviceId, string zoneId, int level)
```

Request to change the load level of a zone on the target controller.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `deviceId` | `string` | The unique ID of the lighting controller to change. |
| `zoneId` | `string` | The unique ID of the zone to change. |
| `level` | `int` | A value from 0–100 representing the load level to set. |

---

### GetActiveScene(string deviceId)

```csharp
string GetActiveScene(string deviceId)
```

Get the currently active scene on the lighting controller.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `deviceId` | `string` | The unique ID of the lighting controller to query. |

**Returns:** The unique ID of the active scene.

---

### GetZoneLoad(string deviceId, string zoneId)

```csharp
int GetZoneLoad(string deviceId, string zoneId)
```

Get the current load level of the target lighting zone.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `deviceId` | `string` | The unique ID of the lighting controller to query. |
| `zoneId` | `string` | The unique ID of the zone to query. |

**Returns:** A value from 0–100 representing the current lighting load level.

---

### GetAllLightingDeviceInfo()

```csharp
ReadOnlyCollection<LightingControlInfoContainer> GetAllLightingDeviceInfo()
```

Get the configuration information for all lighting controllers in the system.

**Returns:** A collection of lighting controller data for all devices in the system.
