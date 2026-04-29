# VideoWallCell

**Namespace:** `gcu_hardware_service.VideoWallDevices`

Data object representing a single window/output cell in a video wall layout.

---

## Table of Contents

**Properties**
- [Id](#id)
- [Label](#label)
- [Icon](#icon)
- [SourceId](#sourceid)
- [DefaultSourceId](#defaultsourceid)
- [XPosition](#xposition)
- [YPosition](#yposition)
- [Tags](#tags)

---

## Properties

### Id

```csharp
public string Id { get; init; }
```

The unique ID of this cell used for internal referencing.

---

### Label

```csharp
public string Label { get; init; }
```

A human-friendly name of the cell.

---

### Icon

```csharp
public string Icon { get; set; }
```

An icon tag used for displaying an image on a UI.

---

### SourceId

```csharp
public string SourceId { get; set; }
```

The unique ID of the currently routed video source.

---

### DefaultSourceId

```csharp
public string DefaultSourceId { get; set; }
```

The ID of the routable video wall source that should be sent to this cell when the parent layout is selected.

---

### XPosition

```csharp
public int XPosition { get; init; }
```

The horizontal location of this cell/window.

---

### YPosition

```csharp
public int YPosition { get; init; }
```

The vertical location of this cell/window.

---

### Tags

```csharp
public List<string> Tags { get; init; }
```

A collection of tags used internally for additional behavior.
