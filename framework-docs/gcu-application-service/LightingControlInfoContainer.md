# LightingControlInfoContainer

**Namespace:** `gcu_application_service.LightingControl`

**Inherits:** [`InfoContainer`](InfoContainer.md)

Data object representing a single lighting controller managed by the application service.

---

## Table of Contents

**Constructors**
- [LightingControlInfoContainer(...)](#lightingcontrolinfocontainer-1)

**Properties**
- [Zones](#zones)
- [Scenes](#scenes)
- [StartupSceneId](#startupsceneid)
- [ShutdownSceneId](#shutdownsceneid)

---

## Constructors

### LightingControlInfoContainer(...)

```csharp
public LightingControlInfoContainer(
    string id,
    string label,
    string icon,
    string startupSceneId,
    string shutdownSceneId,
    List<string> tags,
    List<LightingItemInfoContainer> zones,
    List<LightingItemInfoContainer> scenes)
```

Instantiates a new instance of `LightingControlInfoContainer`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the lighting controller. Used for internal referencing. |
| `label` | `string` | The user-friendly name of the controller. |
| `icon` | `string` | The image tag used for referencing the UI icon. |
| `startupSceneId` | `string` | The ID of the startup scene to recall when prompted. |
| `shutdownSceneId` | `string` | The ID of the shutdown scene to recall when prompted. |
| `tags` | `List<string>` | A collection of custom tags used by the subscribed service. |
| `zones` | `List<LightingItemInfoContainer>` | A collection of data objects for all zones controlled by this device. |
| `scenes` | `List<LightingItemInfoContainer>` | A collection of data objects for all scenes that can be recalled by this device. |

---

## Properties

### Zones

```csharp
public List<LightingItemInfoContainer> Zones { get; }
```

A collection of all zones associated with this lighting controller.

---

### Scenes

```csharp
public List<LightingItemInfoContainer> Scenes { get; }
```

A collection of all scenes associated with this lighting controller.

---

### StartupSceneId

```csharp
public string StartupSceneId { get; }
```

The ID of the lighting scene to recall when the system enters the active state.

---

### ShutdownSceneId

```csharp
public string ShutdownSceneId { get; }
```

The ID of the lighting scene to recall when the system enters the standby state.
