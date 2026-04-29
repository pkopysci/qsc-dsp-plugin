# LightingAttribute

**Namespace:** `gcu_domain_service.Data.LightingData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single lighting zone or scene within a lighting controller. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Label](#label)
- [Index](#index)
- [Tags](#tags)

---

## Properties

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name for this zone or scene, used in UI elements. Defaults to `string.Empty`.

---

### Index

```csharp
public int Index { get; set; }
```

**Type:** `int`

The numeric index used to address this zone or scene in the lighting control protocol. Defaults to `0`.

---

### Tags

```csharp
public List<string> Tags { get; set; }
```

**Type:** `List<string>`

A collection of string tags used to define or filter special behaviors for this zone or scene. Defaults to an empty list.
