# ILightingDevice

**Namespace:** `gcu_hardware_service.LightingDevices`

**Implements:** [`IBaseDevice`](IBaseDevice.md)

Required events, methods, and properties for creating a lighting device control plugin.

---

## Table of Contents

**Events**
- [ZoneLoadChanged](#zoneloadchanged)
- [ActiveSceneChanged](#activescenechanged)

**Properties**
- [ZoneIds](#zoneids)
- [SceneIds](#sceneids)
- [ActiveSceneId](#activesceneid)

**Methods**
- [Initialize(...)](#initialize)
- [AddZone(string id, string label, int index)](#addzonestring-id-string-label-int-index)
- [AddScene(string id, string label, int index)](#addscenestring-id-string-label-int-index)
- [RecallScene(string id)](#recallscenestring-id)
- [SetZoneLoad(string id, int loadLevel)](#setzoneloadstring-id-int-loadlevel)
- [GetZoneLoad(string id)](#getzoneloadstring-id)

---

## Events

### ZoneLoadChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> ZoneLoadChanged
```

Triggered whenever the device reports a change in a zone lighting load. The event arg is the ID of the load that changed.

---

### ActiveSceneChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> ActiveSceneChanged
```

Triggered whenever the device reports a change in the active lighting scene. The event arg is the ID of the scene that was set to active.

---

## Properties

### ZoneIds

```csharp
ReadOnlyCollection<string> ZoneIds { get; }
```

The IDs of all controllable zones for the device.

---

### SceneIds

```csharp
ReadOnlyCollection<string> SceneIds { get; }
```

The IDs of all selectable scenes for the device.

---

### ActiveSceneId

```csharp
string ActiveSceneId { get; }
```

The ID of the currently selected lighting scene.

---

## Methods

### Initialize(...)

```csharp
void Initialize(
    string hostName,
    int port,
    string id,
    string label,
    string userName,
    string password,
    List<string> tags)
```

Connect to the hardware and register any internal event handlers.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `hostName` | `string` | The IP address or hostname used to connect to the hardware. |
| `port` | `int` | The port number used to connect to the hardware. |
| `id` | `string` | A unique ID used to reference this device. |
| `label` | `string` | A human-friendly name of this device. |
| `userName` | `string` | The authentication username used when connecting. |
| `password` | `string` | The authentication password used when connecting. |
| `tags` | `List<string>` | A collection of tags used internally for additional behavior. |

---

### AddZone(string id, string label, int index)

```csharp
void AddZone(string id, string label, int index)
```

Add a controllable zone reference to the device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the zone, used for internal referencing. |
| `label` | `string` | The human-friendly name of the zone. |
| `index` | `int` | The 0-based index of the zone. |

---

### AddScene(string id, string label, int index)

```csharp
void AddScene(string id, string label, int index)
```

Add a selectable scene reference to the device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the scene, used for internal referencing. |
| `label` | `string` | The human-friendly name of the scene. |
| `index` | `int` | The 0-based index of the scene. |

---

### RecallScene(string id)

```csharp
void RecallScene(string id)
```

Send a request to the device to recall the target scene.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the scene to recall. |

---

### SetZoneLoad(string id, int loadLevel)

```csharp
void SetZoneLoad(string id, int loadLevel)
```

Set the load level of the target zone.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the zone to change. |
| `loadLevel` | `int` | A value from 0 to 100 representing the new load level. |

---

### GetZoneLoad(string id)

```csharp
int GetZoneLoad(string id)
```

Get the current load level of the target zone.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the zone to query. |

**Returns:** A value from 0 to 100 representing the current load level.
