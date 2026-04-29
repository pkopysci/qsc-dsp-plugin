# InfrastructureService

**Namespace:** `gcu_hardware_service`

**Implements:** [`IInfrastructureService`](IInfrastructureService.md)

Hardware management service for controlling real-world hardware devices.

---

## Table of Contents

**Constructors**
- [InfrastructureService(CrestronControlSystem controlSystem)](#infrastructureservicecrestroncontrolsystem-controlsystem)

**Properties**
- [Dsps](#dsps)
- [AvSwitchers](#avswitchers)
- [Displays](#displays)
- [Endpoints](#endpoints)
- [CableBoxes](#cableboxes)
- [Blurays](#blurays)
- [LightingDevices](#lightingdevices)
- [VideoWallDevices](#videowalldevices)
- [CameraDevices](#cameradevices)

**Methods**
- [AddDsp(Dsp dsp)](#adddspDsp-dsp)
- [AddAudioChannel(Channel channel)](#addaudiochannelChannel-channel)
- [AddDisplay(Display display)](#adddisplayDisplay-display)
- [AddAvSwitch(MatrixData avSwitch, Routing routingData)](#addavswitchMatrixData-avswitch-Routing-routingdata)
- [AddEndpoint(Endpoint endpointData)](#addendpointEndpoint-endpointdata)
- [AddCableBox(CableBox cableBox)](#addcableboxCableBox-cablebox)
- [AddBluray(Bluray bluray)](#addblurayBluray-bluray)
- [AddLightingDevice(LightingInfo lighting)](#addlightingdeviceLightingInfo-lighting)
- [AddVideoWall(VideoWall videoWall)](#addvideowallVideoWall-videowall)
- [AddCamera(Camera cameraData)](#addcameraCamera-cameradata)
- [ConnectAllDevices()](#connectalldevices)
- [Dispose()](#dispose)

---

## Constructors

### InfrastructureService(CrestronControlSystem controlSystem)

```csharp
public InfrastructureService(CrestronControlSystem controlSystem)
```

Initializes a new instance of the `InfrastructureService` class.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `controlSystem` | `CrestronControlSystem` | A reference to the Crestron control processor running the system. |

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | If `controlSystem` is null. |

---

## Properties

### Dsps

```csharp
public DeviceContainer<IAudioControl> Dsps { get; }
```

Gets a collection of DSP devices that are configured in the system.

---

### AvSwitchers

```csharp
public DeviceContainer<IAvSwitcher> AvSwitchers { get; }
```

Gets a collection of AV switching devices that are configured in the system.

---

### Displays

```csharp
public DeviceContainer<IDisplayDevice> Displays { get; }
```

Gets a collection of display devices that are configured in the system.

---

### Endpoints

```csharp
public DeviceContainer<IEndpointDevice> Endpoints { get; }
```

Gets a collection of endpoints that are configured in the system.

---

### CableBoxes

```csharp
public DeviceContainer<ITransportDevice> CableBoxes { get; }
```

Gets a collection of cable box devices that are in the system configuration.

---

### Blurays

```csharp
public DeviceContainer<ITransportDevice> Blurays { get; }
```

Gets a collection of Blu-ray devices that are in the system configuration.

---

### LightingDevices

```csharp
public DeviceContainer<ILightingDevice> LightingDevices { get; }
```

Gets a collection of lighting controllers that are in the system configuration.

---

### VideoWallDevices

```csharp
public DeviceContainer<IVideoWallDevice> VideoWallDevices { get; }
```

Gets a collection of video wall controllers that are in the system configuration.

---

### CameraDevices

```csharp
public DeviceContainer<ICameraDevice> CameraDevices { get; }
```

Gets a collection of controllable cameras in the system configuration.

---

## Methods

### AddDsp(Dsp dsp)

```csharp
public void AddDsp(Dsp dsp)
```

Add a DSP control object to the current collection. Any DSP in the collection with a matching ID will be replaced. Logs a notice and an error on failure.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `dsp` | `Dsp` | The DSP device to add or replace. |

---

### AddAudioChannel(Channel channel)

```csharp
public void AddAudioChannel(Channel channel)
```

Add an audio input or output channel to a DSP in the current collection. Looks for a DSP with a matching ID and configures it with the channel. Logs an error if no DSP is found or if the channel is missing input/output tags.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `channel` | `Channel` | The channel data object used to configure the DSP control. |

---

### AddDisplay(Display display)

```csharp
public void AddDisplay(Display display)
```

Add a display control object to the current collection. Any display in the collection with a matching ID will be replaced. If the display also implements `IAudioControl`, it is added to the DSP collection as well.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `display` | `Display` | The display device to add or replace. |

---

### AddAvSwitch(MatrixData avSwitch, Routing routingData)

```csharp
public void AddAvSwitch(MatrixData avSwitch, Routing routingData)
```

Add an AV switcher control object to the current collection. Any AV switcher in the collection with a matching ID will be replaced. If the switcher also implements `IAudioControl`, it is added to the DSP collection as well.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `avSwitch` | `MatrixData` | The AV switcher to add or replace. |
| `routingData` | `Routing` | The config data containing all inputs and outputs in the system. |

---

### AddEndpoint(Endpoint endpointData)

```csharp
public void AddEndpoint(Endpoint endpointData)
```

Add an endpoint control object to the current collection and registers it. Any endpoint in the collection with a matching ID shall be replaced.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `endpointData` | `Endpoint` | The endpoint data to add or replace. |

---

### AddCableBox(CableBox cableBox)

```csharp
public void AddCableBox(CableBox cableBox)
```

Add a cable box or satellite TV transport control object to the current collection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `cableBox` | `CableBox` | The cable box or Sat TV transport control config to add. |

---

### AddBluray(Bluray bluray)

```csharp
public void AddBluray(Bluray bluray)
```

Add a Blu-ray transport control object to the current collection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `bluray` | `Bluray` | The Blu-ray transport control config to add. |

---

### AddLightingDevice(LightingInfo lighting)

```csharp
public void AddLightingDevice(LightingInfo lighting)
```

Add a lighting controller to the current collection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `lighting` | `LightingInfo` | The lighting controller config to add. |

---

### AddVideoWall(VideoWall videoWall)

```csharp
public void AddVideoWall(VideoWall videoWall)
```

Add a video wall controller to the current collection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `videoWall` | `VideoWall` | The config data for the video wall device to add. |

---

### AddCamera(Camera cameraData)

```csharp
public void AddCamera(Camera cameraData)
```

Add a camera controller to the current collection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `cameraData` | `Camera` | The config data for the camera device to add. |

---

### ConnectAllDevices()

```csharp
public void ConnectAllDevices()
```

Initialize all hardware connections and call `Connect()` on every device in all device collections.

---

### Dispose()

```csharp
public void Dispose()
```

Releases all managed resources and disposes all device collections.
