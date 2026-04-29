# MatrixEdge

**Namespace:** `gcu_domain_service.Data.RoutingData`

**Inherits:** [`BaseData`](BaseData.md)

Defines a directed edge (connection) between two nodes in the routing graph. Used to model signal flow between matrices or between sources/destinations and matrix ports. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [StartNodeId](#startnodeid)
- [EndNodeId](#endnodeid)

---

## Properties

### StartNodeId

```csharp
public string StartNodeId { get; set; }
```

**Type:** `string`

The ID of the source node (start of the edge) in the routing graph. Defaults to `string.Empty`.

---

### EndNodeId

```csharp
public string EndNodeId { get; set; }
```

**Type:** `string`

The ID of the destination node (end of the edge) in the routing graph. Defaults to `string.Empty`.
