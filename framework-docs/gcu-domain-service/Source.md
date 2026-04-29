# Source

**Namespace:** `gcu_domain_service.Data.RoutingData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single AV routing source. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Static Properties**
- [Empty](#empty)

**Properties**
- [Label](#label)
- [Icon](#icon)
- [Control](#control)
- [Matrix](#matrix)
- [Input](#input)
- [Tags](#tags)

---

## Static Properties

### Empty

```csharp
public static readonly Source Empty
```

**Type:** `Source`

A pre-initialized sentinel instance representing an empty/no-source state. Has the following values set:

| Property | Value |
|----------|-------|
| `Id` | `"SRCEMPTY"` |
| `Label` | `"EMPTY SOURCE"` |
| `Icon` | `"alert"` |
| `Input` | `0` |
| `Control` | `string.Empty` |
| `Matrix` | `string.Empty` |
| `Tags` | Empty list |

---

## Properties

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name for this source, used in the routing UI. Defaults to `string.Empty`.

---

### Icon

```csharp
public string Icon { get; set; }
```

**Type:** `string`

Icon identifier used to represent this source in the UI. Defaults to `string.Empty`.

---

### Control

```csharp
public string Control { get; set; }
```

**Type:** `string`

The ID of the transport device or activity associated with this source (e.g., a Blu-ray player ID or cable box ID). Defaults to `string.Empty`.

---

### Matrix

```csharp
public string Matrix { get; set; }
```

**Type:** `string`

The ID of the [`MatrixData`](MatrixData.md) that routes this source. Defaults to `string.Empty`.

---

### Input

```csharp
public int Input { get; set; }
```

**Type:** `int`

The input number on the matrix that this source is connected to. Defaults to `0`.

---

### Tags

```csharp
public List<string> Tags { get; set; }
```

**Type:** `List<string>`

A collection of string tags used to define or filter special behaviors for this source. Defaults to an empty list.
