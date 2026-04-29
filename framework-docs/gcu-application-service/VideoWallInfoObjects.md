# Video Wall Info Objects

**Namespace:** `gcu_application_service.VideoWallControl`

Data objects used to send video wall state information to subscribers. These types are used as the data payload for the `VideoWallInfoContainer` and events exposed by `IVideoWallApp`.

---

## Table of Contents

- [VideoWallCellInfo](#videowallcellinfo)
- [VideoWallLayoutInfo](#videowallayoutinfo)
- [VideoWallCanvasInfo](#videowallcanvasinfo)
- [VideoWallInfoContainer](#videowallinfoccontainer)

---

## VideoWallCellInfo

```csharp
public record VideoWallCellInfo(
    string Id,
    string Label,
    string Icon,
    int XPosition,
    int YPosition,
    string SourceId)
```

Data object representing a single window/cell within a video wall layout.

**Properties**

| Name | Type | Description |
|------|------|-------------|
| `Id` | `string` | The unique ID of the cell used for internal referencing. |
| `Label` | `string` | The human-friendly name of the cell. |
| `Icon` | `string` | An icon tag used for displaying an image on a UI. |
| `XPosition` | `int` | The horizontal position of this cell in the layout grid. |
| `YPosition` | `int` | The vertical position of this cell in the layout grid. |
| `SourceId` | `string` | The unique ID of the source currently routed to this cell. |

---

## VideoWallLayoutInfo

```csharp
public record VideoWallLayoutInfo(
    string VideoWallControlId,
    int Width,
    int Height,
    string Id,
    string Label,
    string Icon,
    List<VideoWallCellInfo> Cells)
```

Data object representing a single video wall layout.

**Properties**

| Name | Type | Description |
|------|------|-------------|
| `VideoWallControlId` | `string` | The ID of the video wall controller that owns this layout. |
| `Width` | `int` | The number of cells in the layout on the X axis. |
| `Height` | `int` | The number of cells in the layout on the Y axis. |
| `Id` | `string` | The unique ID of this layout. |
| `Label` | `string` | The human-friendly name of this layout. |
| `Icon` | `string` | An icon tag used for displaying an image on a UI. |
| `Cells` | `List<VideoWallCellInfo>` | A collection of all cells and their positions within this layout. |

---

## VideoWallCanvasInfo

```csharp
public record VideoWallCanvasInfo(
    string Id,
    string Label,
    string StartupLayoutId,
    int MaxWidth,
    int MaxHeight,
    List<VideoWallLayoutInfo> Layouts)
```

Data object representing a single video wall canvas.

**Properties**

| Name | Type | Description |
|------|------|-------------|
| `Id` | `string` | The unique ID of the canvas. |
| `Label` | `string` | The human-friendly name of the canvas. |
| `StartupLayoutId` | `string` | The ID of the layout that should be selected on startup. |
| `MaxWidth` | `int` | The total number of possible cells in the X axis. |
| `MaxHeight` | `int` | The total number of possible cells in the Y axis. |
| `Layouts` | `List<VideoWallLayoutInfo>` | A collection of all selectable layouts for this canvas. |

---

## VideoWallInfoContainer

**Inherits:** [`InfoContainer`](InfoContainer.md)

```csharp
public class VideoWallInfoContainer(string id, string label, string icon, List<string> tags, bool isOnline = false)
    : InfoContainer(id, label, icon, tags, isOnline)
```

Top-level data container representing a single video wall controller and all its canvases and sources.

**Parameters**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `id` | `string` | | The unique ID of the controller. Used for internal referencing. |
| `label` | `string` | | The user-friendly name of the controller. |
| `icon` | `string` | | The image tag used for referencing the UI icon. |
| `tags` | `List<string>` | | A collection of custom tags used by the subscribed service. |
| `isOnline` | `bool` | `false` | `true` = device is currently connected; `false` = device offline. |

**Properties**

| Name | Type | Description |
|------|------|-------------|
| `Canvases` | `ReadOnlyCollection<VideoWallCanvasInfo>` | All canvases managed by this controller. |
| `Sources` | `ReadOnlyCollection<AvSourceInfoContainer>` | All video sources routable to this video wall. |
