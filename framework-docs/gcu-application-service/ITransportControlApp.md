# ITransportControlApp

**Namespace:** `gcu_application_service.TransportControl`

Common attributes and methods for controlling one or more transport devices (Blu-ray, cable TV, etc.).

---

## Table of Contents

**Events**
- [BlurayConnectionChanged](#blurayconnectionchanged)
- [CableBoxConnectionChanged](#cableboxconnectionchanged)

**Methods**
- [GetAllCableBoxes()](#getallcableboxes)
- [GetAllBlurays()](#getallblurays)
- [GetCableBox(string id)](#getcableboxstring-id)
- [GetBluray(string id)](#getbluraystring-id)
- [TransportPowerOn(string deviceId)](#transportpoweronstring-deviceid)
- [TransportPowerOff(string deviceId)](#transportpoweroffstring-deviceid)
- [TransportPowerToggle(string deviceId)](#transportpowertogglestring-deviceid)
- [TransportDial(string deviceId, string channel)](#transportdialstring-deviceid-string-channel)
- [TransportDialFavorite(string deviceId, string favoriteId)](#transportdialfavoritestring-deviceid-string-favoriteid)
- [TransportDash(string deviceId)](#transportdashstring-deviceid)
- [TransportChannelUp(string deviceId)](#transportchannelupstring-deviceid)
- [TransportChannelDown(string deviceId)](#transportchanneldownstring-deviceid)
- [TransportPageUp(string deviceId)](#transportpageupstring-deviceid)
- [TransportPageDown(string deviceId)](#transportpagedownstring-deviceid)
- [TransportGuide(string deviceId)](#transportguidestring-deviceid)
- [TransportMenu(string deviceId)](#transportmenustring-deviceid)
- [TransportInfo(string deviceId)](#transportinfostring-deviceid)
- [TransportExit(string deviceId)](#transportexitstring-deviceid)
- [TransportBack(string deviceId)](#transportbackstring-deviceid)
- [TransportPlay(string deviceId)](#transportplaystring-deviceid)
- [TransportPause(string deviceId)](#transportpausestring-deviceid)
- [TransportStop(string deviceId)](#transportstopstring-deviceid)
- [TransportRecord(string deviceId)](#transportrecordstring-deviceid)
- [TransportScanForward(string deviceId)](#transportscanforwardstring-deviceid)
- [TransportScanReverse(string deviceId)](#transportscanreversestring-deviceid)
- [TransportSkipForward(string deviceId)](#transportskipforwardstring-deviceid)
- [TransportSkipReverse(string deviceId)](#transportskipreversestring-deviceid)
- [TransportEject(string deviceId)](#transportejectstring-deviceid)
- [TransportNavUp(string deviceId)](#transportnavupstring-deviceid)
- [TransportNavDown(string deviceId)](#transportnavdownstring-deviceid)
- [TransportNavLeft(string deviceId)](#transportnavleftstring-deviceid)
- [TransportNavRight(string deviceId)](#transportnavrightstring-deviceid)
- [TransportRed(string deviceId)](#transportredstring-deviceid)
- [TransportGreen(string deviceId)](#transportgreenstring-deviceid)
- [TransportYellow(string deviceId)](#transportyellowstring-deviceid)
- [TransportBlue(string deviceId)](#transportbluestring-deviceid)
- [TransportSelect(string deviceId)](#transportselectstring-deviceid)

---

## Events

### BlurayConnectionChanged

```csharp
event EventHandler<GenericDualEventArgs<string, bool>>? BlurayConnectionChanged
```

Triggered when any Blu-ray device reports an online or offline event. `Arg1` is the device ID; `Arg2` is the new connection state.

---

### CableBoxConnectionChanged

```csharp
event EventHandler<GenericDualEventArgs<string, bool>>? CableBoxConnectionChanged
```

Triggered when any cable TV device reports an online or offline event. `Arg1` is the device ID; `Arg2` is the new connection state.

---

## Methods

### GetAllCableBoxes()

```csharp
ReadOnlyCollection<TransportInfoContainer> GetAllCableBoxes()
```

**Returns:** All cable box data objects in the system, or an empty collection if none exist.

---

### GetAllBlurays()

```csharp
ReadOnlyCollection<TransportInfoContainer> GetAllBlurays()
```

**Returns:** All Blu-ray data objects in the system, or an empty collection if none exist.

---

### GetCableBox(string id)

```csharp
TransportInfoContainer GetCableBox(string id)
```

Get information on a single cable box device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the device to query. |

**Returns:** Information on the target cable box, or an empty container if not found.

---

### GetBluray(string id)

```csharp
TransportInfoContainer GetBluray(string id)
```

Get information on a single Blu-ray device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the device to query. |

**Returns:** Information on the target Blu-ray, or an empty container if not found.

---

### TransportPowerOn(string deviceId)

```csharp
void TransportPowerOn(string deviceId)
```

Send the power on command to the target transport device.

---

### TransportPowerOff(string deviceId)

```csharp
void TransportPowerOff(string deviceId)
```

Send the power off command to the target transport device.

---

### TransportPowerToggle(string deviceId)

```csharp
void TransportPowerToggle(string deviceId)
```

Send the power toggle command to the target transport device.

---

### TransportDial(string deviceId, string channel)

```csharp
void TransportDial(string deviceId, string channel)
```

Send a channel string to dial on the target transport device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `deviceId` | `string` | The unique ID of the device to control. |
| `channel` | `string` | The channel string to dial on the device. |

---

### TransportDialFavorite(string deviceId, string favoriteId)

```csharp
void TransportDialFavorite(string deviceId, string favoriteId)
```

Send a request to dial a favorite channel on the device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `deviceId` | `string` | The unique ID of the device to control. |
| `favoriteId` | `string` | The unique ID of the favorite to recall. |

---

### TransportDash(string deviceId)

```csharp
void TransportDash(string deviceId)
```

Send the dash (`-`) command to the target device (used for sub-channel entry).

---

### TransportChannelUp(string deviceId)

```csharp
void TransportChannelUp(string deviceId)
```

Send a command to increase the channel number by 1.

---

### TransportChannelDown(string deviceId)

```csharp
void TransportChannelDown(string deviceId)
```

Send a command to decrease the channel number by 1.

---

### TransportPageUp(string deviceId)

```csharp
void TransportPageUp(string deviceId)
```

Send a command to increase the page or channel listing by 1.

---

### TransportPageDown(string deviceId)

```csharp
void TransportPageDown(string deviceId)
```

Send a command to decrease the page or channel listing by 1.

---

### TransportGuide(string deviceId)

```csharp
void TransportGuide(string deviceId)
```

Send a command to display the guide menu.

---

### TransportMenu(string deviceId)

```csharp
void TransportMenu(string deviceId)
```

Send a command to display the main menu.

---

### TransportInfo(string deviceId)

```csharp
void TransportInfo(string deviceId)
```

Send a command to display the information pop-up.

---

### TransportExit(string deviceId)

```csharp
void TransportExit(string deviceId)
```

Send a command to exit the current menu.

---

### TransportBack(string deviceId)

```csharp
void TransportBack(string deviceId)
```

Send a command to go back one menu or step.

---

### TransportPlay(string deviceId)

```csharp
void TransportPlay(string deviceId)
```

Send a "play" command to the device if supported.

---

### TransportPause(string deviceId)

```csharp
void TransportPause(string deviceId)
```

Send the "pause" command to the device if supported.

---

### TransportStop(string deviceId)

```csharp
void TransportStop(string deviceId)
```

Send the stop command to the device if supported.

---

### TransportRecord(string deviceId)

```csharp
void TransportRecord(string deviceId)
```

Send the record command to the device if supported.

---

### TransportScanForward(string deviceId)

```csharp
void TransportScanForward(string deviceId)
```

Send the scan forward / fast-forward command to the device if supported.

---

### TransportScanReverse(string deviceId)

```csharp
void TransportScanReverse(string deviceId)
```

Send the scan backwards / rewind command to the device if supported.

---

### TransportSkipForward(string deviceId)

```csharp
void TransportSkipForward(string deviceId)
```

Send the Next / Skip Forward command to the device if supported.

---

### TransportSkipReverse(string deviceId)

```csharp
void TransportSkipReverse(string deviceId)
```

Send the back / skip reverse command to the device if supported.

---

### TransportEject(string deviceId)

```csharp
void TransportEject(string deviceId)
```

Send the eject/open tray command to the device if supported.

---

### TransportNavUp(string deviceId)

```csharp
void TransportNavUp(string deviceId)
```

Send the D-pad up / navigate up command to the device.

---

### TransportNavDown(string deviceId)

```csharp
void TransportNavDown(string deviceId)
```

Send the D-pad down / navigate down command to the device.

---

### TransportNavLeft(string deviceId)

```csharp
void TransportNavLeft(string deviceId)
```

Send the D-pad left / navigate left command to the device.

---

### TransportNavRight(string deviceId)

```csharp
void TransportNavRight(string deviceId)
```

Send the D-pad right / navigate right command to the device.

---

### TransportRed(string deviceId)

```csharp
void TransportRed(string deviceId)
```

Send the red (C) color button command to the device.

---

### TransportGreen(string deviceId)

```csharp
void TransportGreen(string deviceId)
```

Send the green (D) color button command to the device.

---

### TransportYellow(string deviceId)

```csharp
void TransportYellow(string deviceId)
```

Send the yellow (A) color button command to the device.

---

### TransportBlue(string deviceId)

```csharp
void TransportBlue(string deviceId)
```

Send the blue (B) color button command to the device.

---

### TransportSelect(string deviceId)

```csharp
void TransportSelect(string deviceId)
```

Send the select/enter command to the device.

All transport method parameters accept a `deviceId` (`string`) — the unique ID of the device to control. Cannot be null or empty.
