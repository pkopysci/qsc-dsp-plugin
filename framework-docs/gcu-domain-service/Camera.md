# Camera

**Namespace:** `gcu_domain_service.Data.CameraData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single camera device. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Label](#label)
- [ClassName](#classname)
- [Connection](#connection)
- [Presets](#presets)
- [Tags](#tags)

---

## Properties

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name for the camera, used in UI elements. Defaults to `string.Empty`.

---

### ClassName

```csharp
public string ClassName { get; set; }
```

**Type:** `string`

The fully qualified class name of the driver or control implementation to load for this camera. Defaults to `string.Empty`.

---

### Connection

```csharp
public Connection Connection { get; set; }
```

**Type:** [`Connection`](Connection.md)

The connection configuration (transport, host, port, credentials) used to control the camera. Defaults to an empty `Connection` instance.

---

### Presets

```csharp
public List<PresetData> Presets { get; set; }
```

**Type:** `List<`[`PresetData`](PresetData.md)`>`

Collection of camera preset positions. Defaults to an empty list.

---

### Tags

```csharp
public List<string> Tags { get; set; }
```

**Type:** `List<string>`

A collection of string tags used to define or filter special behaviors for this camera. Defaults to an empty list.
