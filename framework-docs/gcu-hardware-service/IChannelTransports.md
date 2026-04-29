# IChannelTransports

**Namespace:** `gcu_hardware_service.TransportDevices`

Channel control commands for devices that support channel navigation (cable boxes, set-top boxes, etc.).

---

## Table of Contents

**Methods**
- [Digit(ushort digit)](#digitushort-digit)
- [Dash()](#dash)
- [ChannelUp()](#channelup)
- [ChannelDown()](#channeldown)

---

## Methods

### Digit(ushort digit)

```csharp
void Digit(ushort digit)
```

Send a numeric digit input to the device for channel entry.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `digit` | `ushort` | The numeric digit (0–9) to send. |

---

### Dash()

```csharp
void Dash()
```

Send a dash/separator command for sub-channel entry (e.g., "5-1").

---

### ChannelUp()

```csharp
void ChannelUp()
```

Send a channel up command to the device.

---

### ChannelDown()

```csharp
void ChannelDown()
```

Send a channel down command to the device.
