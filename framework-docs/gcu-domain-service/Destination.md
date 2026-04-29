# Destination

**Namespace:** `gcu_domain_service.Data.RoutingData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single AV routing destination (output). Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Label](#label)
- [Icon](#icon)
- [Matrix](#matrix)
- [Output](#output)
- [RoutingGroup](#routinggroup)
- [Tags](#tags)

---

## Properties

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name for this destination, used in the routing UI. Defaults to `string.Empty`.

---

### Icon

```csharp
public string Icon { get; set; }
```

**Type:** `string`

Icon identifier used to represent this destination in the UI. Defaults to `string.Empty`.

---

### Matrix

```csharp
public string Matrix { get; set; }
```

**Type:** `string`

The ID of the [`MatrixData`](MatrixData.md) that routes to this destination. Defaults to `string.Empty`.

---

### Output

```csharp
public int Output { get; set; }
```

**Type:** `int`

The output number on the matrix that this destination is connected to. Defaults to `0`.

---

### RoutingGroup

```csharp
public int RoutingGroup { get; set; }
```

**Type:** `int`

The routing group index. Destinations in the same group are routed together when a source is selected. Defaults to `0`.

---

### Tags

```csharp
public List<string> Tags { get; set; }
```

**Type:** `List<string>`

A collection of string tags used to define or filter special behaviors for this destination. Defaults to an empty list.
