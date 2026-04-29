# CcdDisplayDevice

**Namespace:** `gcu_hardware_service.DisplayDevices`

**Implements:** [`IDisplayDevice`](IDisplayDevice.md), [`IVideoRoutable`](IVideoRoutable.md), `IDisposable`

Display control object that uses a Crestron Certified Driver (CCD) for control. Also implements [`IVideoBlankDevice`](IVideoBlankDevice.md), [`IVideoFreezeDevice`](IVideoFreezeDevice.md), and [`ISupportsHoursUsed`](ISupportsHoursUsed.md).

---

## Table of Contents

**Constructors**
- [CcdDisplayDevice(IBasicVideoDisplay driver, Display config)](#ccddisplaydeviceibasicvideodisplay-driver-Display-config)

**Events**
- [PowerChanged](#powerchanged)
- [VideoBlankChanged](#videoblankchanged)
- [VideoFreezeChanged](#videofreezechanged)
- [HoursUsedChanged](#hoursusedchanged)
- [VideoRouteChanged](#videoroutechanged)

**Properties**
- [PowerState](#powerstate)
- [BlankState](#blankstate)
- [SupportsFreeze](#supportsfreeze)
- [FreezeState](#freezestate)
- [HoursUsed](#hoursused)
- [EnableReconnect](#enablereconnect)

**Methods**
- [PowerOn()](#poweron)
- [PowerOff()](#poweroff)
- [VideoBlankOn()](#videoblankon)
- [VideoBlankOff()](#videoblankoff)
- [FreezeOn()](#freezeon)
- [FreezeOff()](#freezeoff)
- [EnablePolling()](#enablepolling)
- [DisablePolling()](#disablepolling)
- [Initialize(string host, int port, string label, string id)](#initializestring-host-int-port-string-label-string-id)
- [Connect()](#connect)
- [Disconnect()](#disconnect)
- [GetCurrentVideoSource(uint output)](#getcurrentvideosourceuint-output)
- [RouteVideo(uint source, uint output)](#routevideouint-source-uint-output)
- [ClearVideoRoute(uint output)](#clearvideorouteuint-output)
- [Dispose()](#dispose)

---

## Constructors

### CcdDisplayDevice(IBasicVideoDisplay driver, Display config)

```csharp
public CcdDisplayDevice(IBasicVideoDisplay driver, Display config)
```

Creates a new instance of `CcdDisplayDevice`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `driver` | `IBasicVideoDisplay` | The Crestron Certified Driver object for controlling the device. |
| `config` | `Display` | The device config data created during boot. |

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | If `driver` or `config` is null. |

---

## Events

### PowerChanged

```csharp
public event EventHandler<GenericSingleEventArgs<string>>? PowerChanged
```

Triggered when the power state for the device changes. The event argument contains the device ID.

---

### VideoBlankChanged

```csharp
public event EventHandler<GenericSingleEventArgs<string>>? VideoBlankChanged
```

Triggered when a video blank status is reported by the driver. The event argument contains the device ID.

---

### VideoFreezeChanged

```csharp
public event EventHandler<GenericSingleEventArgs<string>>? VideoFreezeChanged
```

Triggered when the video freeze state changes. The event argument contains the device ID.

---

### HoursUsedChanged

```csharp
public event EventHandler<GenericSingleEventArgs<string>>? HoursUsedChanged
```

Triggered when a lamp hours update is reported by the driver. The event argument contains the device ID.

---

### VideoRouteChanged

```csharp
public event EventHandler<GenericDualEventArgs<string, uint>>? VideoRouteChanged
```

Triggered when a video input change is reported. Event args: arg1 = device ID, arg2 = input number.

---

## Properties

### PowerState

```csharp
public bool PowerState { get; }
```

`true` = device is powered on; `false` = device is powered off or in standby.

---

### BlankState

```csharp
public bool BlankState { get; }
```

`true` = video is blanked; `false` = video is active.

---

### SupportsFreeze

```csharp
public bool SupportsFreeze { get; }
```

`true` = the device supports video freeze via custom commands; `false` = freeze not supported.

---

### FreezeState

```csharp
public bool FreezeState { get; }
```

`true` = video is frozen; `false` = video is in motion.

---

### HoursUsed

```csharp
public uint HoursUsed { get; }
```

Gets the number of lamp hours from the first lamp as reported by the driver. Returns `0` if no lamp data is available.

---

### EnableReconnect

```csharp
public bool EnableReconnect { get; set; }
```

Gets or sets whether the device should attempt to automatically reconnect if the connection is lost.

---

## Methods

### PowerOn()

```csharp
public void PowerOn()
```

Sends a power on command to the display. Logs a warning if the device is not connected.

---

### PowerOff()

```csharp
public void PowerOff()
```

Sends a power off command to the display. Logs a warning if the device is not connected.

---

### VideoBlankOn()

```csharp
public void VideoBlankOn()
```

Enables video mute on the display hardware. Logs a warning if the device is not connected.

---

### VideoBlankOff()

```csharp
public void VideoBlankOff()
```

Disables video mute on the display hardware. Logs a warning if the device is not connected.

---

### FreezeOn()

```csharp
public void FreezeOn()
```

Sends the freeze-on custom command to the display. Sets `FreezeState = true` and raises `VideoFreezeChanged`. Logs a warning if not connected.

---

### FreezeOff()

```csharp
public void FreezeOff()
```

Sends the freeze-off custom command to the display. Sets `FreezeState = false` and raises `VideoFreezeChanged`. Logs a warning if not connected.

---

### EnablePolling()

```csharp
public void EnablePolling()
```

Polling is managed by the underlying CCD; this method is a no-op for CCD devices.

---

### DisablePolling()

```csharp
public void DisablePolling()
```

Polling is managed by the underlying CCD; this method is a no-op for CCD devices.

---

### Initialize(string host, int port, string label, string id)

```csharp
public void Initialize(string host, int port, string label, string id)
```

Sets the `Label` and `Id` properties and subscribes to driver events. Connection parameters are provided but connection itself is established via `Connect()`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `host` | `string` | The IP address or hostname of the device. |
| `port` | `int` | The port number used to connect to the device. |
| `label` | `string` | The user-friendly name of the display. |
| `id` | `string` | The unique ID used when referencing the display. |

---

### Connect()

```csharp
public override void Connect()
```

Calls `Connect()` on the underlying CCD driver.

---

### Disconnect()

```csharp
public override void Disconnect()
```

Calls `Disconnect()` on the underlying CCD driver.

---

### GetCurrentVideoSource(uint output)

```csharp
public uint GetCurrentVideoSource(uint output)
```

Returns the input number currently active on the display. The `output` parameter is unused (displays have a single output). Logs an error if no matching input is found.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `output` | `uint` | Unused; displays have a single output. |

**Returns:** The input number mapped to the currently active `VideoConnections` value, or `0` if not found.

---

### RouteVideo(uint source, uint output)

```csharp
public void RouteVideo(uint source, uint output)
```

Routes the specified input (HDMI, DisplayPort, VGA) to the display. Supported source indexes: 1–9 (HDMI 1–6, DisplayPort 1–2, VGA 1).

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `source` | `uint` | The input number to route (1–9). |
| `output` | `uint` | Unused; displays have a single output. |

---

### ClearVideoRoute(uint output)

```csharp
public void ClearVideoRoute(uint output)
```

Not supported by CCD devices. This method is a no-op.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `output` | `uint` | Unused. |

---

### Dispose()

```csharp
public void Dispose()
```

Stops the offline timer, unsubscribes from driver events, disconnects, and disposes the underlying CCD driver.
