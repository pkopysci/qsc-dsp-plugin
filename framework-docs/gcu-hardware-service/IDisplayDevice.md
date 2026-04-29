# IDisplayDevice

**Namespace:** `gcu_hardware_service.DisplayDevices`

**Implements:** [`IBaseDevice`](IBaseDevice.md), [`IPowerControllable`](IPowerControllable.md)

Common attributes and methods of all video display devices.

---

## Table of Contents

**Properties**
- [EnableReconnect](#enablereconnect)

**Methods**
- [Initialize(string ipAddress, int port, string label, string id)](#initializestring-ipaddress-int-port-string-label-string-id)
- [EnablePolling()](#enablepolling)
- [DisablePolling()](#disablepolling)

---

## Properties

### EnableReconnect

```csharp
bool EnableReconnect { get; set; }
```

Gets or sets a value that indicates whether the object should try to reconnect if disconnected from the hardware for any reason.

---

## Methods

### Initialize(string ipAddress, int port, string label, string id)

```csharp
void Initialize(string ipAddress, int port, string label, string id)
```

Configure the underlying connection of the display.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `ipAddress` | `string` | The IP address or hostname to connect to. |
| `port` | `int` | The port number used to connect to the device. |
| `label` | `string` | The user-friendly name of the display. |
| `id` | `string` | The unique ID of the display used when referencing it for control. |

---

### EnablePolling()

```csharp
void EnablePolling()
```

Allow the display device to poll for current status based on a device-specific interval.

---

### DisablePolling()

```csharp
void DisablePolling()
```

Disable the polling functions if they are enabled.

## Related Types

- [IVideoBlankDevice](IVideoBlankDevice.md) — Optional interface for displays that support video blanking.
- [IVideoFreezeDevice](IVideoFreezeDevice.md) — Optional interface for displays that support video freeze.
- [ISupportsHoursUsed](ISupportsHoursUsed.md) — Optional interface for displays that support lamp/usage hour tracking.
- [IChannelControlDevice](IChannelControlDevice.md) — Optional interface for displays with on-device TV channel control.
- [CcdDisplayDevice](CcdDisplayDevice.md) — Concrete implementation using a Crestron Certified Driver.
