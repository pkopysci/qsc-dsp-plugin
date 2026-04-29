# LightingInfo

**Namespace:** `gcu_domain_service.Data.LightingData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single lighting controller device. Includes the connection details, zones, scenes, and startup/shutdown scene assignments. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Label](#label)
- [ClassName](#classname)
- [StartupSceneId](#startupsceneid)
- [ShutdownSceneId](#shutdownsceneid)
- [Tags](#tags)
- [Connection](#connection)
- [Zones](#zones)
- [Scenes](#scenes)

---

## Properties

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name for the lighting controller, used in UI elements. Defaults to `string.Empty`.

---

### ClassName

```csharp
public string ClassName { get; set; }
```

**Type:** `string`

The fully qualified class name of the driver or implementation to load for this lighting controller. Defaults to `string.Empty`.

---

### StartupSceneId

```csharp
public string StartupSceneId { get; set; }
```

**Type:** `string`

The `Id` of the [`LightingAttribute`](LightingAttribute.md) scene to recall when the system starts up. Defaults to `string.Empty`.

---

### ShutdownSceneId

```csharp
public string ShutdownSceneId { get; set; }
```

**Type:** `string`

The `Id` of the [`LightingAttribute`](LightingAttribute.md) scene to recall when the system shuts down. Defaults to `string.Empty`.

---

### Tags

```csharp
public List<string> Tags { get; set; }
```

**Type:** `List<string>`

A collection of string tags used to define or filter special behaviors for this lighting controller. Defaults to an empty list.

---

### Connection

```csharp
public Connection Connection { get; set; }
```

**Type:** [`Connection`](Connection.md)

The connection configuration (transport, host, port, credentials) used to control the lighting controller. Defaults to an empty `Connection` instance.

---

### Zones

```csharp
public List<LightingAttribute> Zones { get; set; }
```

**Type:** `List<`[`LightingAttribute`](LightingAttribute.md)`>`

Collection of individually addressable lighting zones for this controller. Defaults to an empty list.

---

### Scenes

```csharp
public List<LightingAttribute> Scenes { get; set; }
```

**Type:** `List<`[`LightingAttribute`](LightingAttribute.md)`>`

Collection of lighting scenes (presets) for this controller. Defaults to an empty list.
