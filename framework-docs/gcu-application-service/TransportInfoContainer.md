# TransportInfoContainer

**Namespace:** `gcu_application_service.Base`

**Inherits:** [`InfoContainer`](InfoContainer.md)

Data object for sending information about a transport device (Blu-ray, cable TV, DVD, etc.) to subscribers.

---

## Table of Contents

**Constructors**
- [TransportInfoContainer(...)](#transportinfocontainer-1)

**Properties**
- [SupportsColors](#supportscolors)
- [SupportsDiscretePower](#supportsdiscretepower)
- [SupportsChannelControl](#supportschannelcontrol)
- [SupportsPlaybackControl](#supportsplaybackcontrol)
- [SupportsDigitalCableControl](#supportsdigitalcablecontrol)
- [SupportsEject](#supportseject)
- [SupportsRecord](#supportsrecord)
- [Favorites](#favorites)

## Related Types
- [TransportFavorite](#transportfavorite)

---

## Constructors

### TransportInfoContainer(...)

```csharp
public TransportInfoContainer(
    string id,
    string label,
    string icon,
    List<string> tags,
    bool supportsColors,
    bool supportsDiscretePower,
    List<TransportFavorite> favorites)
```

Instantiates a new instance of `TransportInfoContainer`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the transport device. Used for internal referencing. |
| `label` | `string` | The user-friendly name of the transport device. |
| `icon` | `string` | The image tag used for referencing the UI icon. |
| `tags` | `List<string>` | A collection of custom tags used by the subscribed service. |
| `supportsColors` | `bool` | `true` = enable colored buttons for this device. |
| `supportsDiscretePower` | `bool` | `true` = enable power on/off control; `false` = enable power toggle only. |
| `favorites` | `List<TransportFavorite>` | A collection of favorite channel data objects. |

---

## Properties

### SupportsColors

```csharp
public bool SupportsColors { get; }
```

`true` if this transport device supports color buttons (red, green, blue, yellow).

---

### SupportsDiscretePower

```csharp
public bool SupportsDiscretePower { get; init; }
```

`true` if this transport device supports discrete power on/off; `false` if toggle-only.

---

### SupportsChannelControl

```csharp
public bool SupportsChannelControl { get; init; }
```

`true` if this transport device supports channel controls (dial, channel up, channel down, etc.).

---

### SupportsPlaybackControl

```csharp
public bool SupportsPlaybackControl { get; init; }
```

`true` if this transport device supports play, pause, stop, record, etc.

---

### SupportsDigitalCableControl

```csharp
public bool SupportsDigitalCableControl { get; init; }
```

`true` if this device supports page up/down and menu commands.

---

### SupportsEject

```csharp
public bool SupportsEject { get; init; }
```

`true` if this transport is a playback device with an eject/tray open feature.

---

### SupportsRecord

```csharp
public bool SupportsRecord { get; init; }
```

`true` if this transport supports recording/DVR recording.

---

### Favorites

```csharp
public List<TransportFavorite> Favorites { get; }
```

A collection of data objects containing information on favorite channels for this transport device.

---

## TransportFavorite

**Namespace:** `gcu_application_service.Base`

Data object for storing and sending channel favorite information to subscribers.

### Constructor

```csharp
public TransportFavorite(string id, string label)
```

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the favorite. Used for internal referencing. |
| `label` | `string` | The user-friendly name of the favorite. |

### Properties

| Name | Type | Description |
|------|------|-------------|
| `Id` | `string` | The unique ID of this favorite. |
| `Label` | `string` | The user-friendly name of the favorite. |
