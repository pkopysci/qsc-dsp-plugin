# IInfrastructureService

**Namespace:** `gcu_hardware_service`

**Implements:** `IDisposable`

Properties and methods for the Infrastructure service that provides hardware control.

---

## Table of Contents

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
- [AddLightingDevice(LightingInfo lightingDevice)](#addlightingdeviceLightingInfo-lightingdevice)
- [AddVideoWall(VideoWall videoWall)](#addvideowallVideoWall-videowall)
- [AddCamera(Camera cameraData)](#addcameraCamera-cameradata)
- [ConnectAllDevices()](#connectalldevices)

---

## Properties

### Dsps

```csharp
DeviceContainer<IAudioControl> Dsps { get; }
```

Gets a collection of DSP devices that are configured in the system.

---

### AvSwitchers

```csharp
DeviceContainer<IAvSwitcher> AvSwitchers { get; }
```

Gets a collection of AV switching devices that are configured in the system.

---

### Displays

```csharp
DeviceContainer<IDisplayDevice> Displays { get; }
```

Gets a collection of display devices that are configured in the system.

---

### Endpoints

```csharp
DeviceContainer<IEndpointDevice> Endpoints { get; }
```

Gets a collection of endpoints (RMC-100, CEN-IO, etc.) that are configured in the system.

---

### CableBoxes

```csharp
DeviceContainer<ITransportDevice> CableBoxes { get; }
```

Gets a collection of cable box devices that are in the system configuration.

---

### Blurays

```csharp
DeviceContainer<ITransportDevice> Blurays { get; }
```

Gets a collection of Blu-ray devices that are in the system configuration.

---

### LightingDevices

```csharp
DeviceContainer<ILightingDevice> LightingDevices { get; }
```

Gets a collection of lighting controllers that are in the system configuration.

---

### VideoWallDevices

```csharp
DeviceContainer<IVideoWallDevice> VideoWallDevices { get; }
```

Gets a collection of video wall controllers that are in the system configuration.

---

### CameraDevices

```csharp
DeviceContainer<ICameraDevice> CameraDevices { get; }
```

Gets a collection of controllable cameras in the system configuration.

---

## Methods

### AddDsp(Dsp dsp)

```csharp
void AddDsp(Dsp dsp)
```

Add a DSP control object to the current collection. Any DSP in the collection with a matching ID will be replaced.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `dsp` | `Dsp` | The DSP device to add or replace. |

---

### AddAudioChannel(Channel channel)

```csharp
void AddAudioChannel(Channel channel)
```

Add an audio input or output channel to a DSP in the current collection. This will look for a DSP with a matching ID and then configure that device with the channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `channel` | `Channel` | The channel data object used to configure the DSP control. |

---

### AddDisplay(Display display)

```csharp
void AddDisplay(Display display)
```

Add a display control object to the current collection. Any display in the collection with a matching ID will be replaced.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `display` | `Display` | The display device to add or replace. |

---

### AddAvSwitch(MatrixData avSwitch, Routing routingData)

```csharp
void AddAvSwitch(MatrixData avSwitch, Routing routingData)
```

Add an AV switcher control object to the current collection. Any AV switcher in the collection with a matching ID will be replaced.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `avSwitch` | `MatrixData` | The AV switcher to add or replace. |
| `routingData` | `Routing` | The config data containing all inputs and outputs in the system. |

---

### AddEndpoint(Endpoint endpointData)

```csharp
void AddEndpoint(Endpoint endpointData)
```

Add an endpoint control object to the current collection. Any endpoint in the collection with a matching ID shall be replaced.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `endpointData` | `Endpoint` | The endpoint data to add or replace. |

---

### AddCableBox(CableBox cableBox)

```csharp
void AddCableBox(CableBox cableBox)
```

Add a cable box or satellite TV transport control object to the current collection. Any connection in the collection with a matching ID shall be replaced.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `cableBox` | `CableBox` | The cable box or Sat TV transport control config to add. |

---

### AddBluray(Bluray bluray)

```csharp
void AddBluray(Bluray bluray)
```

Add a Blu-ray transport control object to the current collection. Any connection in the collection with a matching ID shall be replaced.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `bluray` | `Bluray` | The Blu-ray transport control config to add. |

---

### AddLightingDevice(LightingInfo lightingDevice)

```csharp
void AddLightingDevice(LightingInfo lightingDevice)
```

Add a lighting controller to the current collection. Any connection in the collection with a matching ID shall be replaced.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `lightingDevice` | `LightingInfo` | The lighting controller to add. |

---

### AddVideoWall(VideoWall videoWall)

```csharp
void AddVideoWall(VideoWall videoWall)
```

Add a video wall controller to the current collection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `videoWall` | `VideoWall` | The config data for the video wall device to add. |

---

### AddCamera(Camera cameraData)

```csharp
void AddCamera(Camera cameraData)
```

Add a camera controller to the current collection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `cameraData` | `Camera` | The config data for the camera device to add. |

---

### ConnectAllDevices()

```csharp
void ConnectAllDevices()
```

Initialize all hardware connections and connect to the devices for control.
