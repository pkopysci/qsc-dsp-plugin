# DomainService

**Namespace:** `gcu_domain_service`

**Implements:** [`IDomainService`](IDomainService.md)

Implementation of the Domain hardware provider service. Provides read-only access to all hardware configuration data parsed from a [`DataContainer`](DataContainer.md). All collection properties return an empty read-only list rather than null when the underlying configuration data is missing.

---

## Table of Contents

**Constructors**
- [DomainService()](#domainservice)
- [DomainService(DataContainer configuration)](#domainservicedatacontainer-configuration)

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

## Constructors

### DomainService()

```csharp
public DomainService()
```

Initializes a new instance of `DomainService` with an empty `DataContainer`.

---

### DomainService(DataContainer configuration)

```csharp
public DomainService(DataContainer configuration)
```

Initializes a new instance of `DomainService` with the provided configuration object.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `configuration` | `DataContainer` | The configuration object representing the system setup. |

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | If `configuration` is null. |

---

## Properties

### Displays

```csharp
public ReadOnlyCollection<Display> Displays { get; }
```

Gets a read-only collection of all display devices defined in the configuration. Returns an empty collection if no displays are configured.

---

### Dsps

```csharp
public ReadOnlyCollection<Dsp> Dsps { get; }
```

Gets a read-only collection of all DSP devices defined in the configuration. Returns an empty collection if no DSPs are configured.

---

### AudioChannels

```csharp
public ReadOnlyCollection<Channel> AudioChannels { get; }
```

Gets a read-only collection of all audio channels defined in the configuration. Returns an empty collection if no channels are configured.

---

### Cameras

```csharp
public ReadOnlyCollection<Camera> Cameras { get; }
```

Gets a read-only collection of all camera devices defined in the configuration. Returns an empty collection if no cameras are configured.

---

### Lighting

```csharp
public ReadOnlyCollection<LightingInfo> Lighting { get; }
```

Gets a read-only collection of all lighting controller data defined in the configuration. Returns an empty collection if no lighting controllers are configured.

---

### UserInterfaces

```csharp
public ReadOnlyCollection<UserInterface> UserInterfaces { get; }
```

Gets a read-only collection of all UI data models defined in the configuration. Returns an empty collection if no user interfaces are configured.

---

### Endpoints

```csharp
public ReadOnlyCollection<Endpoint> Endpoints { get; }
```

Gets a read-only collection of all AV endpoints defined in the configuration. Returns an empty collection if no endpoints are configured.

---

### Blurays

```csharp
public ReadOnlyCollection<Bluray> Blurays { get; }
```

Gets a read-only collection of all Blu-ray devices defined in the configuration. Returns an empty collection if no Blu-rays are configured.

---

### CableBoxes

```csharp
public ReadOnlyCollection<CableBox> CableBoxes { get; }
```

Gets a read-only collection of all cable box devices defined in the configuration. Returns an empty collection if no cable boxes are configured.

---

### VideoWalls

```csharp
public ReadOnlyCollection<VideoWall> VideoWalls { get; }
```

Gets a read-only collection of all video wall controllers defined in the configuration. Returns an empty collection if no video walls are configured.

---

### Fusion

```csharp
public FusionInfo Fusion { get; }
```

Gets the Fusion configuration data defined in the config file. Returns an empty `FusionInfo` object if no fusion data is configured.

---

### RoutingInfo

```csharp
public Routing RoutingInfo { get; }
```

Gets the routing map defined in the configuration file. Returns an empty `Routing` object if no routing data is configured.

---

### RoomInfo

```csharp
public RoomInfo RoomInfo { get; }
```

Gets the basic room information as defined in the configuration file. Returns an empty `RoomInfo` object if no room info is configured.

---

### ServerInfo

```csharp
public ServerInfo ServerInfo { get; }
```

Gets the remote dependency server information as defined in the configuration file. Returns an empty `ServerInfo` object if no server info is configured.

---

## Methods

### GetDisplay(string id)

```csharp
public Display GetDisplay(string id)
```

Search through all displays for one with a matching ID. Writes a warning to the logging system if not found.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the display to search for. |

**Returns:** The first matching `Display`, or an empty `Display` object.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `id` is null or empty. |

---

### GetDsp(string id)

```csharp
public Dsp GetDsp(string id)
```

Search through all DSPs for one with a matching ID. Writes a warning to the logging system if not found.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the DSP to search for. |

**Returns:** The first matching `Dsp`, or an empty `Dsp` object.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `id` is null or empty. |

---

### GetCamera(string id)

```csharp
public Camera GetCamera(string id)
```

Search through all cameras for one with a matching ID. Writes a warning to the logging system if not found.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the camera to search for. |

**Returns:** The first matching `Camera`, or an empty `Camera` object.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `id` is null or empty. |

---

### GetLightingInfo(string id)

```csharp
public LightingInfo GetLightingInfo(string id)
```

Search through all lighting controllers for one with a matching ID. Writes a warning to the logging system if not found.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the lighting controller to search for. |

**Returns:** The first matching `LightingInfo`, or an empty `LightingInfo` object.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `id` is null or empty. |

---

### GetUserInterface(string id)

```csharp
public UserInterface GetUserInterface(string id)
```

Search through all user interfaces for one with a matching ID. Writes a warning to the logging system if not found.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the user interface to search for. |

**Returns:** The first matching `UserInterface`, or an empty `UserInterface` object.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `id` is null or empty. |

---

### GetEndpoint(string id)

```csharp
public Endpoint GetEndpoint(string id)
```

Search through all AV endpoints for one with a matching ID. Writes a warning to the logging system if not found.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the AV endpoint to search for. |

**Returns:** The first matching `Endpoint`, or an empty `Endpoint` object.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `id` is null or empty. |

---

### GetBluray(string id)

```csharp
public Bluray GetBluray(string id)
```

Search through all Blu-ray devices for one with a matching ID. Writes a warning to the logging system if not found.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the Blu-ray to search for. |

**Returns:** The first matching `Bluray`, or an empty `Bluray` object.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `id` is null or empty. |

---

### GetCableBox(string id)

```csharp
public CableBox GetCableBox(string id)
```

Search through all cable box devices for one with a matching ID. Writes a warning to the logging system if not found.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the cable box to search for. |

**Returns:** The first matching `CableBox`, or an empty `CableBox` object.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `id` is null or empty. |

---

### GetVideoWall(string id)

```csharp
public VideoWall GetVideoWall(string id)
```

Search through all video walls for one with a matching ID. Writes a warning to the logging system if not found.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the video wall to search for. |

**Returns:** The first matching `VideoWall`, or an empty `VideoWall` object.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `id` is null or empty. |
