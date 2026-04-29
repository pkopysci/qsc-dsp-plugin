# PresetData

**Namespace:** `gcu_domain_service.Data.CameraData`

Configuration data for a single camera preset position.

---

## Table of Contents

**Properties**
- [Id](#id)
- [Label](#label)
- [Number](#number)

---

## Properties

### Id

```csharp
public string Id { get; set; }
```

**Type:** `string`

Unique identifier for the preset. Used to reference the preset programmatically. Defaults to `string.Empty`.

---

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name for the preset, used in UI elements. Defaults to `string.Empty`.

---

### Number

```csharp
public int Number { get; set; }
```

**Type:** `int`

The numeric index of the preset as used by the camera driver or control protocol. Defaults to `0`.
