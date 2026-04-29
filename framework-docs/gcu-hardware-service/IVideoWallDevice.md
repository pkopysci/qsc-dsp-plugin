# IVideoWallDevice

**Namespace:** `gcu_hardware_service.VideoWallDevices`

**Implements:** [`IBaseDevice`](IBaseDevice.md)

Required events, properties, and methods for the framework to support a video wall controller.

---

## Table of Contents

**Events**
- [VideoWallLayoutChanged](#videowallchanged)
- [VideoWallCellSourceChanged](#videowallcellsourcechanged)

**Properties**
- [Canvases](#canvases)
- [Sources](#sources)

**Methods**
- [Initialize(...)](#initialize)
- [SetActiveLayout(string canvasId, string layoutId)](#setactivelayoutstring-canvasid-string-layoutid)
- [GetActiveLayoutId(string canvasId)](#getactivelayoutidstring-canvasid)
- [SetCellSource(string canvasId, string cellId, string sourceId)](#setcellsourcestring-canvasid-string-cellid-string-sourceid)
- [GetCellSourceId(string canvasId, string cellId)](#getcellsourceidstring-canvasid-string-cellid)

---

## Events

### VideoWallLayoutChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> VideoWallLayoutChanged
```

Triggered whenever the device reports that the active layout has changed. The event arg is the ID of the canvas that changed.

---

### VideoWallCellSourceChanged

```csharp
event EventHandler<GenericDualEventArgs<string, string>> VideoWallCellSourceChanged
```

Triggered whenever a cell's routed source changes. `Arg1` is the ID of the canvas that changed; `Arg2` is the ID of the cell in the active layout that changed.

---

## Properties

### Canvases

```csharp
List<VideoWallCanvas> Canvases { get; }
```

A collection of all [`VideoWallCanvas`](VideoWallCanvas.md) objects that are selectable by this controller.

---

### Sources

```csharp
List<Source> Sources { get; }
```

A collection of all `Source` objects that are routable to video wall cells.

---

## Methods

### Initialize(...)

```csharp
void Initialize(
    string hostname,
    int port,
    string id,
    string label,
    string username,
    string password)
```

Register all internal components and define connection information.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `hostname` | `string` | The IP address or hostname used to connect. |
| `port` | `int` | The port number used to connect. |
| `id` | `string` | The unique ID of the device. |
| `label` | `string` | The user-friendly name of the device. |
| `username` | `string` | The authentication username used to connect. |
| `password` | `string` | The authentication password used to connect. |

---

### SetActiveLayout(string canvasId, string layoutId)

```csharp
void SetActiveLayout(string canvasId, string layoutId)
```

Send a layout change command to a canvas on the video wall controller.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `canvasId` | `string` | The unique ID of the canvas to change. |
| `layoutId` | `string` | The unique ID of the layout to select. |

---

### GetActiveLayoutId(string canvasId)

```csharp
string GetActiveLayoutId(string canvasId)
```

Get the ID of the currently selected layout on a canvas.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `canvasId` | `string` | The unique ID of the canvas to query. |

**Returns:** The ID of the currently selected layout.

---

### SetCellSource(string canvasId, string cellId, string sourceId)

```csharp
void SetCellSource(string canvasId, string cellId, string sourceId)
```

Route a video source to a cell in the currently selected layout.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `canvasId` | `string` | The unique ID of the canvas on which to change the route. |
| `cellId` | `string` | The unique ID of the cell to route to. |
| `sourceId` | `string` | The unique ID of the source being routed. |

---

### GetCellSourceId(string canvasId, string cellId)

```csharp
string GetCellSourceId(string canvasId, string cellId)
```

Query the controller for the currently routed source on a cell.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `canvasId` | `string` | The unique ID of the canvas to query. |
| `cellId` | `string` | The unique ID of the cell in the currently active layout. |

**Returns:** The unique ID of the source routed to the queried cell.
