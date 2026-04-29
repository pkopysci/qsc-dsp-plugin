# Routing

**Namespace:** `gcu_domain_service.Data.RoutingData`

**Inherits:** [`BaseData`](BaseData.md)

Root configuration object for the AV routing system. Contains all sources, destinations, matrix devices, and routing graph edges. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [RouteOnSourceSelect](#routeonsourceselect)
- [StartupSource](#startupsource)
- [Sources](#sources)
- [Destinations](#destinations)
- [MatrixData](#matrixdata)
- [MatrixEdges](#matrixedges)

---

## Properties

### RouteOnSourceSelect

```csharp
public bool RouteOnSourceSelect { get; set; }
```

**Type:** `bool`

When `true`, the system will automatically route the selected source to all configured destinations when a source is selected. Defaults to `false`.

---

### StartupSource

```csharp
public string StartupSource { get; set; }
```

**Type:** `string`

The `Id` of the [`Source`](Source.md) to route automatically when the system starts up. Defaults to `string.Empty`.

---

### Sources

```csharp
public List<Source> Sources { get; set; }
```

**Type:** `List<`[`Source`](Source.md)`>`

Collection of all available AV input sources in the system. Defaults to an empty list.

---

### Destinations

```csharp
public List<Destination> Destinations { get; set; }
```

**Type:** `List<`[`Destination`](Destination.md)`>`

Collection of all AV output destinations (e.g., displays, projectors) in the system. Defaults to an empty list.

---

### MatrixData

```csharp
public List<MatrixData> MatrixData { get; set; }
```

**Type:** `List<`[`MatrixData`](MatrixData.md)`>`

Collection of matrix switcher configurations in the system. Defaults to an empty list.

---

### MatrixEdges

```csharp
public List<MatrixEdge> MatrixEdges { get; set; }
```

**Type:** `List<`[`MatrixEdge`](MatrixEdge.md)`>`

Collection of directed edges defining the routing graph connections between matrix nodes. Defaults to an empty list.
