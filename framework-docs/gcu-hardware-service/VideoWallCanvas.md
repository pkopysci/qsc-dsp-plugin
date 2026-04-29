# VideoWallCanvas

**Namespace:** `gcu_hardware_service.VideoWallDevices`

Data object representing a single video wall canvas that contains multiple layouts.

---

## Table of Contents

**Properties**
- [Id](#id)
- [Label](#label)
- [StartupLayoutId](#startuplayoutid)
- [Layouts](#layouts)
- [MaxHeight](#maxheight)
- [MaxWidth](#maxwidth)
- [Tags](#tags)

---

## Properties

### Id

```csharp
public string Id { get; init; }
```

The unique ID of the canvas, used for runtime referencing.

---

### Label

```csharp
public string Label { get; init; }
```

A human-friendly name of the canvas.

---

### StartupLayoutId

```csharp
public string StartupLayoutId { get; init; }
```

The unique ID of the layout that should be triggered on startup.

---

### Layouts

```csharp
public List<VideoWallLayout> Layouts { get; init; }
```

A collection of selectable [`VideoWallLayout`](VideoWallLayout.md) objects associated with this canvas.

---

### MaxHeight

```csharp
public int MaxHeight { get; init; }
```

The total number of possible cells in the X axis.

---

### MaxWidth

```csharp
public int MaxWidth { get; init; }
```

The total number of possible cells in the Y axis.

---

### Tags

```csharp
public List<string> Tags { get; init; }
```

A collection of tags used internally for additional behavior.
