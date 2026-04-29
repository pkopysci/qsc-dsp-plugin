# DataContainer

**Namespace:** `gcu_domain_service.Data`

Object representation of the JSON configuration file. This class is the root deserialization target when loading system configuration via [`DomainFactory.CreateDomainFromJson()`](DomainFactory.md). All properties default to empty instances so the object is always in a usable state even if the source JSON is incomplete.

---

## Table of Contents

**Properties**
- [ServerInfo](#serverinfo)
- [RoomInfo](#roominfo)
- [UserInterfaces](#userinterfaces)
- [Displays](#displays)
- [Routing](#routing)
- [Audio](#audio)
- [Cameras](#cameras)
- [Blurays](#blurays)
- [CableBoxes](#cableboxes)
- [LightingControllers](#lightingcontrollers)
- [Endpoints](#endpoints)
- [VideoWalls](#videowalls)
- [FusionInfo](#fusioninfo)

---

## Properties

### ServerInfo

```csharp
public ServerInfo ServerInfo { get; set; }
```

**Type:** [`ServerInfo`](ServerInfo.md)

Remote dependency server connection information. Defaults to an empty `ServerInfo` instance.

---

### RoomInfo

```csharp
public RoomInfo RoomInfo { get; set; }
```

**Type:** [`RoomInfo`](RoomInfo.md)

Basic room identification and behavior configuration. Defaults to an empty `RoomInfo` instance.

---

### UserInterfaces

```csharp
public List<UserInterface> UserInterfaces { get; set; }
```

**Type:** `List<`[`UserInterface`](UserInterface.md)`>`

Collection of user interface panel configurations. Defaults to an empty list.

---

### Displays

```csharp
public List<Display> Displays { get; set; }
```

**Type:** `List<`[`Display`](Display.md)`>`

Collection of display device configurations. Defaults to an empty list.

---

### Routing

```csharp
public Routing Routing { get; set; }
```

**Type:** [`Routing`](Routing.md)

Audio/video routing map including sources, destinations, and matrix data. Defaults to an empty `Routing` instance.

---

### Audio

```csharp
public Audio Audio { get; set; }
```

**Type:** [`Audio`](Audio.md)

DSP device and channel configuration. Defaults to an empty `Audio` instance.

---

### Cameras

```csharp
public List<Camera> Cameras { get; set; }
```

**Type:** `List<`[`Camera`](Camera.md)`>`

Collection of camera device configurations. Defaults to an empty list.

---

### Blurays

```csharp
public List<Bluray> Blurays { get; set; }
```

**Type:** `List<`[`Bluray`](Bluray.md)`>`

Collection of Blu-ray player configurations. Defaults to an empty list.

---

### CableBoxes

```csharp
public List<CableBox> CableBoxes { get; set; }
```

**Type:** `List<`[`CableBox`](CableBox.md)`>`

Collection of cable/satellite box configurations. Defaults to an empty list.

---

### LightingControllers

```csharp
public List<LightingInfo> LightingControllers { get; set; }
```

**Type:** `List<`[`LightingInfo`](LightingInfo.md)`>`

Collection of lighting controller configurations. Defaults to an empty list.

---

### Endpoints

```csharp
public List<Endpoint> Endpoints { get; set; }
```

**Type:** `List<`[`Endpoint`](Endpoint.md)`>`

Collection of AV endpoint configurations. Defaults to an empty list.

---

### VideoWalls

```csharp
public List<VideoWall> VideoWalls { get; set; }
```

**Type:** `List<`[`VideoWall`](VideoWall.md)`>`

Collection of video wall controller configurations. Defaults to an empty list.

---

### FusionInfo

```csharp
public FusionInfo FusionInfo { get; set; }
```

**Type:** [`FusionInfo`](FusionInfo.md)

Crestron Fusion room monitoring configuration. Defaults to an empty `FusionInfo` instance.
