# IVideoWallApp

**Namespace:** `gcu_application_service.VideoWallControl`

Required events, methods, and properties for managing video wall devices.

---

## Table of Contents

**Events**
- [VideoWallLayoutChanged](#videowallchanged)
- [VideoWallCellRouteChanged](#videowallcellroutechanged)
- [VideoWallConnectionStatusChanged](#videowallconnectionstatuschanged)

**Methods**
- [GetAllVideoWalls()](#getallvideowalls)
- [QueryAllVideoWallSources(string controlId)](#queryallvideowallsourcesstring-controlid)
- [QueryVideoWallConnectionStatus(string controlId)](#queryvideowallconnectionstatusstring-controlid)
- [QueryActiveVideoWallLayout(string controlId, string canvasId)](#queryactivevideowallayoutstring-controlid-string-canvasid)
- [QueryVideoWallCellSource(string controlId, string canvasId, string cellId)](#queryvideowallcellsourcestring-controlid-string-canvasid-string-cellid)
- [SetActiveVideoWallLayout(string controlId, string canvasId, string layoutId)](#setactivevideowallayoutstring-controlid-string-canvasid-string-layoutid)
- [SetVideoWallCellRoute(string controlId, string canvasId, string cellId, string sourceId)](#setvideowallcellroutestring-controlid-string-canvasid-string-cellid-string-sourceid)

---

## Events

### VideoWallLayoutChanged

```csharp
event EventHandler<GenericDualEventArgs<string, string>> VideoWallLayoutChanged
```

Triggered when the active layout changes on a video wall canvas. `Arg1` is the controller ID; `Arg2` is the canvas ID.

---

### VideoWallCellRouteChanged

```csharp
event EventHandler<GenericTrippleEventArgs<string, string, string>> VideoWallCellRouteChanged
```

Triggered when the source routed to a cell/window changes. `Arg1` is the video wall controller ID; `Arg2` is the canvas ID that changed; `Arg3` is the cell ID in the active layout that changed.

---

### VideoWallConnectionStatusChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> VideoWallConnectionStatusChanged
```

Triggered when the connection status of a video wall controller changes. The event arg is the controller ID.

---

## Methods

### GetAllVideoWalls()

```csharp
ReadOnlyCollection<VideoWallInfoContainer> GetAllVideoWalls()
```

**Returns:** A collection of data objects representing all controllable video wall devices.

---

### QueryAllVideoWallSources(string controlId)

```csharp
List<AvSourceInfoContainer> QueryAllVideoWallSources(string controlId)
```

Get a collection of all sources available to the target video wall controller.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `controlId` | `string` | The ID of the video wall controller to query. |

**Returns:** A collection of all source data available to the controller, or an empty list if no wall is found.

---

### QueryVideoWallConnectionStatus(string controlId)

```csharp
bool QueryVideoWallConnectionStatus(string controlId)
```

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `controlId` | `string` | The ID of the video wall controller to query. |

**Returns:** `true` if the device is online; `false` if offline.

---

### QueryActiveVideoWallLayout(string controlId, string canvasId)

```csharp
string QueryActiveVideoWallLayout(string controlId, string canvasId)
```

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `controlId` | `string` | The ID of the video wall controller to query. |
| `canvasId` | `string` | The ID of the canvas to query. |

**Returns:** The ID of the currently selected layout.

---

### QueryVideoWallCellSource(string controlId, string canvasId, string cellId)

```csharp
string QueryVideoWallCellSource(string controlId, string canvasId, string cellId)
```

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `controlId` | `string` | The ID of the video wall controller to query. |
| `canvasId` | `string` | The ID of the canvas to query. |
| `cellId` | `string` | The ID of the cell in the active layout. |

**Returns:** The ID of the source currently routed to the cell/window.

---

### SetActiveVideoWallLayout(string controlId, string canvasId, string layoutId)

```csharp
void SetActiveVideoWallLayout(string controlId, string canvasId, string layoutId)
```

Send a request to the hardware service to select a new layout.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `controlId` | `string` | The ID of the video wall controller to change. |
| `canvasId` | `string` | The ID of the canvas to change. |
| `layoutId` | `string` | The ID of the new layout to set as active. |

---

### SetVideoWallCellRoute(string controlId, string canvasId, string cellId, string sourceId)

```csharp
void SetVideoWallCellRoute(string controlId, string canvasId, string cellId, string sourceId)
```

Route a video source to a cell/window in the active layout.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `controlId` | `string` | The ID of the video wall controller to change. |
| `canvasId` | `string` | The ID of the canvas to change. |
| `cellId` | `string` | The ID of the cell/window in the active layout to change. |
| `sourceId` | `string` | The ID of the video source to route to the cell/window. |
