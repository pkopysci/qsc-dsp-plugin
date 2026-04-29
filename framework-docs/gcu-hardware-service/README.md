# gcu-hardware-service

Documentation for the `gcu-hardware-service` library. This library provides interfaces and implementations for controlling AV hardware devices in Crestron control systems.

---

## Core

| Type | Description |
|------|-------------|
| [ICrestronDevice](ICrestronDevice.md) | Interface for any device that requires a Crestron control system reference. |
| [IInfrastructureService](IInfrastructureService.md) | Top-level service interface for managing all hardware device collections. |
| [InfrastructureService](InfrastructureService.md) | Concrete implementation of `IInfrastructureService`. |
| [InfrastructureServiceFactory](InfrastructureServiceFactory.md) | Static factory for creating `IInfrastructureService` instances. |

---

## BaseDevice

| Type | Description |
|------|-------------|
| [IBaseDevice](IBaseDevice.md) | Foundation interface for all hardware device plugins. |
| [BaseDevice](BaseDevice.md) | Abstract base class implementing `IBaseDevice`. |
| [DeviceContainer](DeviceContainer.md) | Generic manager class for storing and retrieving device instances. |

---

## Common Interfaces

| Type | Description |
|------|-------------|
| [IPowerControllable](IPowerControllable.md) | Interface for devices that support discrete power control. |
| [IBackupSupport](IRedundancySupport.md) | Interface for devices that support redundancy/backup switching. |
| [IVideoRoutable](IVideoRoutable.md) | Interface for devices that support video routing. |
| [IAudioRoutable](IAudioRoutable.md) | Interface for devices that support audio routing. |

---

## Communication

| Type | Description |
|------|-------------|
| [ITcpDevice](ITcpDevice.md) | Interface for TCP/IP-controlled devices. |
| [ISerialDevice](ISerialDevice.md) | Interface for RS-232 serial-controlled devices. |
| [IIrDevice](IIrDevice.md) | Interface for IR-controlled devices. |
| [IWakeOnLanDevice](IWakeOnLanDevice.md) | Interface for devices that require Wake-On-LAN to power on. |

---

## AudioDevices

| Type | Description |
|------|-------------|
| [IAudioControl](IAudioControl.md) | Interface for DSP audio zone and level control. |
| [IDsp](IDsp.md) | Plugin interface for DSP devices; extends `IAudioControl`. |
| [IAudioZoneEnabler](IAudioZoneEnabler.md) | Interface for enabling and disabling audio zones. |
| [IDspLogicTriggerSupport](IDspLogicTriggerSupport.md) | Interface for DSP logic trigger control. |

---

## AvSwitchDevices

| Type | Description |
|------|-------------|
| [IAvSwitcher](IAvSwitcher.md) | Interface for AV switcher devices. |
| [IVideoInputSyncDevice](IVideoInputSyncDevice.md) | Interface for devices that report video input sync state. |
| [AvSwitchCommands](AvSwitchCommands.md) | Enum of AV switcher command identifiers. |

---

## AvIpMatrix

| Type | Description |
|------|-------------|
| [IAvIpMatrix](IAvIpMatrix.md) | Interface for AV-over-IP matrix controllers. |
| [IAvIpEndpoint](IAvIpEndpoint.md) | Interface for a single AV-IP encoder or decoder endpoint. |
| [AvIpEndpointTypes](AvIpEndpointTypes.md) | Enum defining AV-IP endpoint types. |

---

## CameraDevices

| Type | Description |
|------|-------------|
| [ICameraDevice](ICameraDevice.md) | Plugin interface for PTZ camera devices. |
| [IPanTiltDevice](IPanTiltDevice.md) | Interface for cameras that support pan/tilt controls. |
| [IZoomDevice](IZoomDevice.md) | Interface for cameras that support zoom controls. |
| [IPresetDevice](IPresetDevice.md) | Interface for cameras that support preset recall and save. |
| [CameraPreset](CameraPreset.md) | Data type for a single camera preset. |

---

## DisplayDevices

| Type | Description |
|------|-------------|
| [IDisplayDevice](IDisplayDevice.md) | Plugin interface for display devices. |
| [IVideoBlankDevice](IVideoBlankDevice.md) | Interface for displays that support video blank. |
| [IVideoFreezeDevice](IVideoFreezeDevice.md) | Interface for displays that support video freeze. |
| [ISupportsHoursUsed](ISupportsHoursUsed.md) | Interface for displays that report hours-used data. |
| [IChannelControlDevice](IChannelControlDevice.md) | Interface for displays that support channel up/down. |
| [CcdDisplayDevice](CcdDisplayDevice.md) | CCD-based display device plugin implementation. |

---

## EndpointDevices

| Type | Description |
|------|-------------|
| [IEndpointDevice](IEndpointDevice.md) | Interface for endpoint devices (DM-TX, RMC-100, etc.). |
| [IRelayDevice](IRelayDevice.md) | Interface for relay control. |
| [ISerialEndpoint](ISerialEndpoint.md) | Interface for endpoints that support RS-232 output. |
| [IIrEndpoint](IIrEndpoint.md) | Interface for endpoints that support IR output. |
| [ProcessorEndpoint](ProcessorEndpoint.md) | Endpoint wrapper for the Crestron control processor's onboard ports. |
| [C2NIoRelayDevice](C2NIoRelayDevice.md) | Crestron C2N-IO relay controller via Cresnet. |
| [CenIoRy401RelayDevice](CenIoRy401RelayDevice.md) | Crestron CEN-IO-RY-401 relay controller via Ethernet. |

---

## LightingDevices

| Type | Description |
|------|-------------|
| [ILightingDevice](ILightingDevice.md) | Plugin interface for lighting control devices. |

---

## TransportDevices

| Type | Description |
|------|-------------|
| [ITransportDevice](ITransportDevice.md) | Interface for transport devices (Blu-ray, DVD, etc.) with navigation controls. |
| [IPlaybackTransports](IPlaybackTransports.md) | Interface for playback controls (play, pause, stop, etc.). |
| [IChannelTransports](IChannelTransports.md) | Interface for channel navigation controls. |
| [IColorButtonTransports](IColorButtonTransports.md) | Interface for color button controls (red, green, yellow, blue). |
| [IDigitalCableTransports](IDigitalCableTransports.md) | Interface for digital cable box-specific controls. |

---

## VideoWallDevices

| Type | Description |
|------|-------------|
| [IVideoWallDevice](IVideoWallDevice.md) | Plugin interface for video wall controllers. |
| [VideoWallCanvas](VideoWallCanvas.md) | Data object for a video wall canvas containing multiple layouts. |
| [VideoWallLayout](VideoWallLayout.md) | Data object for a single video wall layout. |
| [VideoWallCell](VideoWallCell.md) | Data object for a single cell/window in a video wall layout. |
