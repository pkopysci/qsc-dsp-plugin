# VideoWallLayout

**Namespace:** `gcu_hardware_service.VideoWallDevices`

Data object representing a single video wall layout.

---

## Table of Contents

**Properties**
- [Id](#id)
- [Label](#label)
- [Icon](#icon)
- [Width](#width)
- [Height](#height)
- [Cells](#cells)
- [Tags](#tags)

---

## Properties

### Id

```csharp
public string Id { get; init; }
```

The unique ID of this layout used for internal referencing.

---

### Label

```csharp
public string Label { get; init; }
```

The human-friendly name of this layout.

---

### Icon

```csharp
public string Icon { get; init; }
```

An icon tag used to display an associated image on the UI.

---

### Width

```csharp
public int Width { get; init; }
```

The number of cells in the layout on the X axis.

---

### Height

```csharp
public int Height { get; init; }
```

The number of cells in the layout on the Y axis.

---

### Cells

```csharp
public List<VideoWallCell> Cells { get; init; }
```

A collection of all [`VideoWallCell`](VideoWallCell.md) objects and their positions within this layout.

---

### Tags

```csharp
public List<string> Tags { get; init; }
```

A collection of tags used internally for additional behavior.
