# VideoWall

**Namespace:** `gcu_domain_service.Data.VideoWallData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single video wall controller device. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Label](#label)
- [ClassName](#classname)
- [Tags](#tags)
- [Connection](#connection)

---

## Properties

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name for the video wall controller, used in UI elements. Defaults to `string.Empty`.

---

### ClassName

```csharp
public string ClassName { get; set; }
```

**Type:** `string`

The fully qualified class name of the driver or control implementation to load for this video wall. Defaults to `string.Empty`.

---

### Tags

```csharp
public List<string> Tags { get; set; }
```

**Type:** `List<string>`

A collection of string tags used to define or filter special behaviors for this video wall. Defaults to an empty list.

---

### Connection

```csharp
public Connection Connection { get; set; }
```

**Type:** [`Connection`](Connection.md)

The connection configuration (transport, host, port, credentials) used to control the video wall. Defaults to an empty `Connection` instance.
