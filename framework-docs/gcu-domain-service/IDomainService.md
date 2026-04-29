# IDomainService

**Namespace:** `gcu_domain_service`

Interface defining the common properties and methods for the Domain hardware provider service. This is the primary contract for accessing all hardware configuration data deserialized from a JSON configuration file.

---

## Table of Contents

**Properties**
- [Displays](#displays)
- [Dsps](#dsps)
- [AudioChannels](#audiochannels)
- [Cameras](#cameras)
- [Lighting](#lighting)
- [UserInterfaces](#userinterfaces)
- [Endpoints](#endpoints)
- [Blurays](#blurays)
- [CableBoxes](#cableboxes)
- [VideoWalls](#videowalls)
- [Fusion](#fusion)
- [RoutingInfo](#routinginfo)
- [RoomInfo](#roominfo)
- [ServerInfo](#serverinfo)

**Methods**
- [GetDisplay(string id)](#getdisplaystring-id)
- [GetDsp(string id)](#getdspstring-id)
- [GetCamera(string id)](#getcamerastring-id)
- [GetLightingInfo(string id)](#getlightinginfostring-id)
- [GetUserInterface(string id)](#getuserinterfacestring-id)
- [GetEndpoint(string id)](#getendpointstring-id)
- [GetBluray(string id)](#getbluraystring-id)
- [GetCableBox(string id)](#getcableboxstring-id)
- [GetVideoWall(string id)](#getvideowall-string-id)

---

## Properties

### Displays

```csharp
ReadOnlyCollection<Display> Displays { get; }
```

Gets a collection of all display devices defined in the configuration.

---

### Dsps

```csharp
ReadOnlyCollection<Dsp> Dsps { get; }
```

Gets a collection of all DSP devices defined in the configuration.

---

### AudioChannels

```csharp
ReadOnlyCollection<Channel> AudioChannels { get; }
```

Gets a collection of all audio channels defined in the configuration.

---

### Cameras

```csharp
ReadOnlyCollection<Camera> Cameras { get; }
```

Gets a collection of all camera devices defined in the configuration.

---

### Lighting

```csharp
ReadOnlyCollection<LightingInfo> Lighting { get; }
```

Gets a collection of all lighting data defined in the configuration.

---

### UserInterfaces

```csharp
ReadOnlyCollection<UserInterface> UserInterfaces { get; }
```

Gets a collection of all UI data models defined in the configuration.

---

### Endpoints

```csharp
ReadOnlyCollection<Endpoint> Endpoints { get; }
```

Gets a collection of all audio/video endpoints defined in the configuration.

---

### Blurays

```csharp
ReadOnlyCollection<Bluray> Blurays { get; }
```

Gets a collection of all Blu-ray devices defined in the configuration.

---

### CableBoxes

```csharp
ReadOnlyCollection<CableBox> CableBoxes { get; }
```

Gets a collection of all cable box devices defined in the configuration.

---

### VideoWalls

```csharp
ReadOnlyCollection<VideoWall> VideoWalls { get; }
```

Gets a collection of all video wall controllers defined in the configuration.

---

### Fusion

```csharp
FusionInfo Fusion { get; }
```

Gets the Fusion configuration data defined in the config file.

---

### RoutingInfo

```csharp
Routing RoutingInfo { get; }
```

Gets the routing map defined in the configuration file.

---

### RoomInfo

```csharp
RoomInfo RoomInfo { get; }
```

Gets the basic room information as defined in the configuration file.

---

### ServerInfo

```csharp
ServerInfo ServerInfo { get; }
```

Gets the remote dependency server information as defined in the configuration file.

---

## Methods

### GetDisplay(string id)

```csharp
Display GetDisplay(string id)
```

Search through all displays in the configuration for one with an ID that matches `id`. If a display cannot be found, a warning is written to the logging system.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the display to search for. |

**Returns:** The first `Display` instance that matches `id`, or an empty `Display` object if none is found.

---

### GetDsp(string id)

```csharp
Dsp GetDsp(string id)
```

Search through all DSPs in the configuration for one with an ID that matches `id`. If a DSP cannot be found, a warning is written to the logging system.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the DSP to search for. |

**Returns:** The first `Dsp` instance that matches `id`, or an empty `Dsp` object if none is found.

---

### GetCamera(string id)

```csharp
Camera GetCamera(string id)
```

Search through all cameras in the configuration for one with an ID that matches `id`. If a camera cannot be found, a warning is written to the logging system.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the camera to search for. |

**Returns:** The first `Camera` instance that matches `id`, or an empty `Camera` object if none is found.

---

### GetLightingInfo(string id)

```csharp
LightingInfo GetLightingInfo(string id)
```

Search through all lights in the configuration for one with an ID that matches `id`. If a light cannot be found, a warning is written to the logging system.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the lighting controller to search for. |

**Returns:** The first `LightingInfo` instance that matches `id`, or an empty `LightingInfo` object if none is found.

---

### GetUserInterface(string id)

```csharp
UserInterface GetUserInterface(string id)
```

Search through all user interfaces in the configuration for one with an ID that matches `id`. If a user interface cannot be found, a warning is written to the logging system.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the user interface to search for. |

**Returns:** The first `UserInterface` instance that matches `id`, or an empty `UserInterface` object if none is found.

---

### GetEndpoint(string id)

```csharp
Endpoint GetEndpoint(string id)
```

Search through all AV endpoints in the configuration for one with an ID that matches `id`. If an AV endpoint cannot be found, a warning is written to the logging system.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the AV endpoint to search for. |

**Returns:** The first `Endpoint` instance that matches `id`, or an empty `Endpoint` object if none is found.

---

### GetBluray(string id)

```csharp
Bluray GetBluray(string id)
```

Search through all Blu-rays in the configuration for one with an ID that matches `id`. If a Blu-ray cannot be found, a warning is written to the logging system.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the Blu-ray to search for. |

**Returns:** The first `Bluray` instance that matches `id`, or an empty `Bluray` object if none is found.

---

### GetCableBox(string id)

```csharp
CableBox GetCableBox(string id)
```

Search through all cable boxes in the configuration for one with an ID that matches `id`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the cable box to search for. |

**Returns:** The first `CableBox` instance that matches `id`, or an empty `CableBox` object if none is found.

---

### GetVideoWall(string id)

```csharp
VideoWall GetVideoWall(string id)
```

Search through all video walls in the configuration for one with a matching ID.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the video wall to search for. |

**Returns:** The first `VideoWall` instance that matches `id`, or an empty `VideoWall` object if none is found.
