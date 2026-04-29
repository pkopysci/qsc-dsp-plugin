# gcu-application-service

Documentation for the `gcu-application-service` library. This library provides the application-level control layer that sits between the hardware service and the presentation/user interface layer in the GCU AV framework.

---

## Core

| Type | Description |
|------|-------------|
| [IApplicationService](IApplicationService.md) | Top-level service interface; aggregates all subsystem control interfaces. |
| [ApplicationService](ApplicationService.md) | Default implementation of `IApplicationService`; extendable by plugins. |
| [ApplicationControlFactory](ApplicationControlFactory.md) | Static factory for creating `IApplicationService` instances. |
| [ITechAuthGroupAppService](ITechAuthGroupAppService.md) | Optional interface for technician-level UI lockout support. |

---

## Base

| Type | Description |
|------|-------------|
| [InfoContainer](InfoContainer.md) | Base data container with common attributes for all device data objects. |
| [BaseApp\<TDevice, TDeviceData\>](BaseApp.md) | Generic base class pairing a hardware device collection with configuration data. |
| [RedundantDeviceInfoContainer](RedundantDeviceInfoContainer.md) | Data object for devices that support backup/redundancy switching. |
| [RoomInfoContainer](RoomInfoContainer.md) | Data object for general room information. |
| [TransportInfoContainer](TransportInfoContainer.md) | Data object for transport devices (Blu-ray, cable TV, etc.) including `TransportFavorite`. |

---

## AudioControl

| Type | Description |
|------|-------------|
| [IAudioControlApp](IAudioControlApp.md) | Interface for audio channel level, mute, route, and zone enable control. |
| [IAudioPresetApp](IAudioPresetApp.md) | Interface for DSP audio preset recall. |
| [IAudioRedundancyApp](IAudioRedundancyApp.md) | Interface for audio devices that support backup/redundant switching. |
| [AudioChannelInfoContainer](AudioChannelInfoContainer.md) | Data object for a single audio channel including zone enable controls. |
| [DspInfoContainer](DspInfoContainer.md) | Data object for a DSP device including its presets. |

---

## AvRouting

| Type | Description |
|------|-------------|
| [IAvRoutingApp](IAvRoutingApp.md) | Interface for AV source routing management. |
| [IAvIpMatrixApp](IAvIpMatrixApp.md) | Interface for AV-over-IP matrix routing. |
| [AvSourceInfoContainer](AvSourceInfoContainer.md) | Data object for a single AV source input. |
| [AvIpEndpointInfoContainer](AvIpEndpointInfoContainer.md) | Data object for an AV-over-IP encoder or decoder endpoint. |

---

## CameraControl

| Type | Description |
|------|-------------|
| [ICameraControlApp](ICameraControlApp.md) | Interface for PTZ camera pan/tilt/zoom/preset control. |
| [CameraInfoContainer](CameraInfoContainer.md) | Data object for a single controllable camera device. |

---

## CustomEvents

| Type | Description |
|------|-------------|
| [ICustomEventAppService](ICustomEventAppService.md) | Interface for non-standard custom event behaviors in a plugin application service. |
| [CustomEventAppService](CustomEventAppService.md) | Abstract base class extending `ApplicationService` with custom event support. |
| [CustomEventInfoContainer](CustomEventInfoContainer.md) | Data object for a single custom event. |

---

## DisplayControl

| Type | Description |
|------|-------------|
| [IDisplayControlApp](IDisplayControlApp.md) | Interface for display power, blank, freeze, screen, and input control. |
| [DisplayInfoContainer](DisplayInfoContainer.md) | Data object for a display device. |
| [DisplayInput](DisplayInput.md) | Data object for a single selectable input on a display. |

---

## EndpointControl

| Type | Description |
|------|-------------|
| [IEndpointControlApp](IEndpointControlApp.md) | Interface for relay and connection management on endpoint devices. |

---

## LightingControl

| Type | Description |
|------|-------------|
| [ILightingControlApp](ILightingControlApp.md) | Interface for lighting scene recall and zone load control. |
| [LightingControlInfoContainer](LightingControlInfoContainer.md) | Data object for a lighting controller including zones and scenes. |
| [LightingItemInfoContainer](LightingItemInfoContainer.md) | Data object for a single lighting zone or scene item. |

---

## SystemPower

| Type | Description |
|------|-------------|
| [ISystemPowerApp](ISystemPowerApp.md) | Interface for AV system active/standby power state management. |

---

## TransportControl

| Type | Description |
|------|-------------|
| [ITransportControlApp](ITransportControlApp.md) | Interface for all transport device commands (power, navigation, playback, etc.). |

---

## UserInterface

| Type | Description |
|------|-------------|
| [UserInterfaceDataContainer](UserInterfaceDataContainer.md) | Data object for a touch panel or other user interface. |
| [MenuItemDataContainer](MenuItemDataContainer.md) | Data object for a single UI menu item. |

---

## VideoWallControl

| Type | Description |
|------|-------------|
| [IVideoWallApp](IVideoWallApp.md) | Interface for video wall layout and cell source routing. |
| [VideoWallInfoObjects](VideoWallInfoObjects.md) | Data types: `VideoWallInfoContainer`, `VideoWallCanvasInfo`, `VideoWallLayoutInfo`, `VideoWallCellInfo`. |
