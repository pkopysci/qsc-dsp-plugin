# IDisplayControlApp

**Namespace:** `gcu_application_service.DisplayControl`

Common properties and methods for display control applications.

---

## Table of Contents

**Events**
- [DisplayPowerChange](#displaypowerchange)
- [DisplayBlankChange](#displayblankchange)
- [DisplayFreezeChange](#displayfreezechange)
- [DisplayConnectChange](#displayconnectchange)
- [DisplayInputChanged](#displayinputchanged)

**Methods**
- [SetDisplayPower(string id, bool newState)](#setdisplaypowerstring-id-bool-newstate)
- [DisplayPowerQuery(string id)](#displaypowerquerystring-id)
- [DisplayInputLecternQuery(string id)](#displayinputlecternquerystring-id)
- [DisplayInputStationQuery(string id)](#displayinputstationquerystring-id)
- [SetDisplayBlank(string id, bool newState)](#setdisplayblankstring-id-bool-newstate)
- [DisplayBlankQuery(string id)](#displayblankquerystring-id)
- [SetDisplayFreeze(string id, bool state)](#setdisplayfreezestring-id-bool-state)
- [DisplayFreezeQuery(string id)](#displayfreezequerystring-id)
- [RaiseScreen(string displayId)](#raisescreenstring-displayid)
- [LowerScreen(string displayId)](#lowerscreenstring-displayid)
- [SetInputLectern(string displayId)](#setinputlecternstring-displayid)
- [SetInputStation(string displayId)](#setinputstationstring-displayid)
- [SetDisplayChannelUp(string displayId)](#setdisplaychannelupstring-displayid)
- [SetDisplayChannelDown(string displayId)](#setdisplaychanneldownstring-displayid)
- [GetAllDisplayInfo()](#getalldisplayinfo)

---

## Events

### DisplayPowerChange

```csharp
event EventHandler<GenericDualEventArgs<string, bool>> DisplayPowerChange
```

Triggered whenever a display reports a change in power status. `Arg1` is the display ID; `Arg2` is the new power state.

---

### DisplayBlankChange

```csharp
event EventHandler<GenericDualEventArgs<string, bool>> DisplayBlankChange
```

Triggered whenever a display reports a change in video blank status. `Arg1` is the display ID; `Arg2` is the new blank state.

---

### DisplayFreezeChange

```csharp
event EventHandler<GenericDualEventArgs<string, bool>> DisplayFreezeChange
```

Triggered whenever a display reports a change in video freeze status. `Arg1` is the display ID; `Arg2` is the new freeze state.

---

### DisplayConnectChange

```csharp
event EventHandler<GenericDualEventArgs<string, bool>> DisplayConnectChange
```

Triggered whenever a display reports a change in connection status. `Arg1` is the display ID; `Arg2` is the connection state.

---

### DisplayInputChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> DisplayInputChanged
```

Triggered whenever a display indicates the on-device input selection has changed. Only fires for displays that implement `IVideoRoutable`. The event arg is the display ID.

---

## Methods

### SetDisplayPower(string id, bool newState)

```csharp
void SetDisplayPower(string id, bool newState)
```

Request to change the power state of the target display.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the display to change. |
| `newState` | `bool` | `true` = turn on; `false` = turn off. |

---

### DisplayPowerQuery(string id)

```csharp
bool DisplayPowerQuery(string id)
```

Query the current power state of the target display.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the display to query. |

**Returns:** `true` if display power is on; `false` if off.

---

### DisplayInputLecternQuery(string id)

```csharp
bool DisplayInputLecternQuery(string id)
```

Query whether the target routable display has the lectern input source selected.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the display to query. |

**Returns:** `true` if the display is `IVideoRoutable` and the lectern source is selected; `false` otherwise.

---

### DisplayInputStationQuery(string id)

```csharp
bool DisplayInputStationQuery(string id)
```

Query whether the target routable display has the station input source selected.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the display to query. |

**Returns:** `true` if the display is `IVideoRoutable` and the station source is selected; `false` otherwise.

---

### SetDisplayBlank(string id, bool newState)

```csharp
void SetDisplayBlank(string id, bool newState)
```

Request to change the video blank state of the target display.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the display to change. |
| `newState` | `bool` | `true` = set blank on; `false` = set blank off. |

---

### DisplayBlankQuery(string id)

```csharp
bool DisplayBlankQuery(string id)
```

Query the current video blank status of the target display.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the display to query. |

**Returns:** `true` if video is currently blanked; `false` otherwise (or if display not found).

---

### SetDisplayFreeze(string id, bool state)

```csharp
void SetDisplayFreeze(string id, bool state)
```

Request to change the video freeze state of the target display.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the display to change. |
| `state` | `bool` | `true` = set freeze on; `false` = set freeze off. |

---

### DisplayFreezeQuery(string id)

```csharp
bool DisplayFreezeQuery(string id)
```

Query the current video freeze state of the target display.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the display to query. |

**Returns:** `true` if video is frozen; `false` if motion is active.

---

### RaiseScreen(string displayId)

```csharp
void RaiseScreen(string displayId)
```

Request to raise the relay-controlled screen associated with the target display.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `displayId` | `string` | The unique ID of the display associated with the screen being raised. |

---

### LowerScreen(string displayId)

```csharp
void LowerScreen(string displayId)
```

Request to lower the relay-controlled screen associated with the target display.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `displayId` | `string` | The unique ID of the display associated with the screen being lowered. |

---

### SetInputLectern(string displayId)

```csharp
void SetInputLectern(string displayId)
```

Request to set the target display to the lectern source defined in the configuration. Does nothing if the display does not implement `IVideoRoutable`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `displayId` | `string` | The target display to attempt to change. |

---

### SetInputStation(string displayId)

```csharp
void SetInputStation(string displayId)
```

Request to set the target display to the local station source defined in the configuration. Does nothing if the display does not implement `IVideoRoutable`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `displayId` | `string` | The target display to attempt to change. |

---

### SetDisplayChannelUp(string displayId)

```csharp
void SetDisplayChannelUp(string displayId)
```

Send a command to increase the current channel on the target display. Does nothing if the display does not support channel control.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `displayId` | `string` | The unique ID of the display to control. |

---

### SetDisplayChannelDown(string displayId)

```csharp
void SetDisplayChannelDown(string displayId)
```

Send a command to decrease the current channel on the target display. Does nothing if the display does not support channel control.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `displayId` | `string` | The unique ID of the display to control. |

---

### GetAllDisplayInfo()

```csharp
ReadOnlyCollection<DisplayInfoContainer> GetAllDisplayInfo()
```

Get the configuration information for all displays in the system.

**Returns:** A collection of display data for all displays in the system.
