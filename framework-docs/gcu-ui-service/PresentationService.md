# PresentationService

**Namespace:** `gcu_ui_service`

**Implements:** [`IPresentationService`](IPresentationService.md), `IDisposable`

Root presentation layer implementation. Orchestrates all user interface connections, Fusion server integration, and application service event subscriptions. Creates `IUserInterface` plugin objects from configuration data, subscribes to `IApplicationService` events, and bridges Fusion server commands to the application service. Designed to be extended — all event handlers and orchestration methods are `protected virtual`.

---

## Table of Contents

**Constructors**
- [PresentationService(IApplicationService appService, CrestronControlSystem control)](#presentationserviceiapplicationservice-appservice-crestroncontrolsystem-control)

**Protected Fields**
- [Control](#control)
- [AppService](#appservice)
- [UiConnections](#uiconnections)
- [Fusion](#fusion)
- [Disposed](#disposed)

**Methods**
- [Initialize()](#initialize)
- [Dispose()](#dispose)
- [Dispose(bool disposing)](#disposebool-disposing)
- [BuildInterfaces()](#buildinterfaces)
- [SubscribeToAppService()](#subscribetoappservice)
- [UnsubscribeFromAppService()](#unsubscribefromappservice)
- [SubscribeToInterface(IUserInterface ui)](#subscribetointerfaceiuserinterface-ui)
- [UnsubscribeFromInterfaces()](#unsubscribefrominterfaces)
- [CameraAppConnectionChangeHandler(...)](#cameraappconnectionchangehandler)
- [VideoWallAppConnectionChangeHandler(...)](#videowallappconnectionchangehandler)
- [AppServiceDspConnectionHandler(...)](#appservicedspconnectionhandler)
- [AppServiceAudioInputMuteHandler(...)](#appserviceaudioinputmutehandler)
- [AppServiceAudioOutputLevelHandler(...)](#appserviceaudiooutputlevelhandler)
- [AppServiceAudioOutputMuteHandler(...)](#appserviceaudiooutputmutehandler)
- [AppServiceDisplayConnectionHandler(...)](#appservicedisplayconnectionhandler)
- [AppServiceDisplayPowerHandler(...)](#appservicedisplaypowerhandler)
- [AppServiceEndpointConnectionHandler(...)](#appserviceendpointconnectionhandler)
- [ApplicationCableTvConnectionHandler(...)](#applicationcabletvconnectionhandler)
- [ApplicationBlurayConnectionHandler(...)](#applicationblurayconnectionhandler)
- [AppServiceRouteHandler(...)](#appserviceroutehandler)
- [AppServiceRouterConnectionHandler(...)](#appservicerouterconnectionhandler)
- [AppServiceLightingConnectionHandler(...)](#appservicelightingconnectionhandler)
- [AppServiceGlobalFreezeHandler(...)](#appserviceglobalfreezehandler)
- [AppServiceGlobalBlankHandler(...)](#appserviceglobalblankhandler)
- [AppServiceStateChangeHandler(...)](#appservicestatechangehandler)
- [UiConnectionHandler(...)](#uiconnectionhandler)
- [FusionRouteSourceHandler(...)](#fusionroutesourcehandler)
- [FusionAudioLevelHandler(...)](#fusionaudiolevelhandler)
- [FusionAudioMuteHandler(...)](#fusionaudiomutehandler)
- [FusionDisplayFreezeHandler(...)](#fusiondisplayfreezehandler)
- [FusionDisplayBlankHandler(...)](#fusiondisplayblankhandler)
- [FusionDisplayPowerHandler(...)](#fusiondisplaypowerhandler)
- [FusionPowerHandler(...)](#fusionpowerhandler)
- [FusionMicMuteHandler(...)](#fusionmicmutehandler)
- [FusionConnectionHandler(...)](#fusionconnectionhandler)
- [UpdateFusionDisplayPowerFeedback()](#updatefusiondisplaypowerfeedback)
- [UpdateFusionAudioFeedback()](#updatefusionautiofeedback)
- [UpdateFusionRoutingFeedback()](#updatefusionroutingfeedback)
- [UpdateFusionFeedback()](#updatefusionfeedback)

---

## Constructors

### PresentationService(IApplicationService appService, CrestronControlSystem control)

```csharp
public PresentationService(IApplicationService appService, CrestronControlSystem control)
```

Instantiates a new instance of `PresentationService`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `appService` | `IApplicationService` | The framework application service implementation that handles state management. |
| `control` | `CrestronControlSystem` | The root Crestron control system object. |

---

## Protected Fields

### Control

```csharp
protected readonly CrestronControlSystem Control
```

Root control system running the application.

---

### AppService

```csharp
protected readonly IApplicationService AppService
```

Core application service for managing business logic.

---

### UiConnections

```csharp
protected readonly List<IUserInterface> UiConnections
```

All user interfaces in the system configuration.

---

### Fusion

```csharp
protected IFusionInterface? Fusion
```

The Fusion room connection implementation.

---

### Disposed

```csharp
protected bool Disposed
```

`true` = object is disposed, `false` = not disposed.

---

## Methods

### Initialize()

```csharp
public virtual void Initialize()
```

Calls `BuildInterfaces()`, subscribes to the application service via `SubscribeToAppService()`, calls `Connect()` on each UI connection, and calls `Fusion.Initialize()`.

---

### Dispose()

```csharp
public void Dispose()
```

Disposes of all managed resources by calling `Dispose(true)`.

---

### Dispose(bool disposing)

```csharp
protected virtual void Dispose(bool disposing)
```

Disposes of all internal component objects if they are disposable. Unsubscribes from all application service and UI events, disposes of each `IUserInterface` that implements `IDisposable`, and unregisters all Fusion event handlers before disposing the Fusion connection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `disposing` | `bool` | `true` if called from `Dispose()`, `false` if called from the finalizer. |

---

### BuildInterfaces()

```csharp
protected virtual void BuildInterfaces()
```

Iterates through all user interface definitions in the application service and creates the associated plugin objects and event subscriptions. Also calls `PresentationServiceFactory.CreateFusionService()` and subscribes all Fusion event handlers.

---

### SubscribeToAppService()

```csharp
protected virtual void SubscribeToAppService()
```

Subscribes to all application service events for all implemented application interfaces. Conditionally subscribes to optional interfaces including `IVideoWallApp`, `ICameraControlApp`, and `IAudioRedundancyApp` if the application service implements them.

---

### UnsubscribeFromAppService()

```csharp
protected virtual void UnsubscribeFromAppService()
```

Unsubscribes from all application service event handlers added by `SubscribeToAppService()`.

---

### SubscribeToInterface(IUserInterface ui)

```csharp
protected virtual void SubscribeToInterface(IUserInterface ui)
```

Subscribe to all user interface events for all implemented plugin interfaces.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `ui` | `IUserInterface` | The user interface plugin to subscribe to. |

---

### UnsubscribeFromInterfaces()

```csharp
protected virtual void UnsubscribeFromInterfaces()
```

Unsubscribes from all event handlers added by `BuildInterfaces()`.

---

### CameraAppConnectionChangeHandler(...)

```csharp
protected virtual void CameraAppConnectionChangeHandler(object? sender, GenericSingleEventArgs<string> args)
```

Handle camera connection change notifications from the application service. Clears or adds a Fusion offline error based on the new camera connection state.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `args` | `GenericSingleEventArgs<string>` | `args.Arg` = the ID of the camera that updated. |

---

### VideoWallAppConnectionChangeHandler(...)

```csharp
protected virtual void VideoWallAppConnectionChangeHandler(object? sender, GenericSingleEventArgs<string> args)
```

Handle video wall device connection status notifications from the application service.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `args` | `GenericSingleEventArgs<string>` | `args.Arg` = the ID of the video wall that updated. |

---

### AppServiceDspConnectionHandler(...)

```csharp
protected virtual void AppServiceDspConnectionHandler(object? sender, GenericSingleEventArgs<string> args)
```

Handle notifications from the application service about an audio DSP changing connection status.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `args` | `GenericSingleEventArgs<string>` | `args.Arg` = the ID of the DSP that changed. |

---

### AppServiceAudioInputMuteHandler(...)

```csharp
protected virtual void AppServiceAudioInputMuteHandler(object? sender, GenericSingleEventArgs<string> args)
```

Handle notifications from the application service about mute state changes on audio inputs. Updates the Fusion mic mute state for the changed channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `args` | `GenericSingleEventArgs<string>` | `args.Arg` = the ID of the audio channel that changed. |

---

### AppServiceAudioOutputLevelHandler(...)

```csharp
protected virtual void AppServiceAudioOutputLevelHandler(object? sender, GenericSingleEventArgs<string> args)
```

Handle notifications from the application service about volume level changes on audio outputs. Calls `UpdateFusionAudioFeedback()`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `args` | `GenericSingleEventArgs<string>` | `args.Arg` = the ID of the audio channel that changed. |

---

### AppServiceAudioOutputMuteHandler(...)

```csharp
protected virtual void AppServiceAudioOutputMuteHandler(object? sender, GenericSingleEventArgs<string> args)
```

Handle notifications from the application service about mute state changes on audio outputs. Calls `UpdateFusionAudioFeedback()`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `args` | `GenericSingleEventArgs<string>` | `args.Arg` = the ID of the audio channel that changed. |

---

### AppServiceDisplayConnectionHandler(...)

```csharp
protected virtual void AppServiceDisplayConnectionHandler(object? sender, GenericDualEventArgs<string, bool> args)
```

Handle notifications about video display or projector connection status changes. Clears or adds a Fusion offline error based on the new connection state.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `args` | `GenericDualEventArgs<string, bool>` | `Arg1` = the ID of the display that changed; `Arg2` = `true` if online, `false` if offline. |

---

### AppServiceDisplayPowerHandler(...)

```csharp
protected virtual void AppServiceDisplayPowerHandler(object? sender, GenericDualEventArgs<string, bool> e)
```

Handle notifications about video display or projector power status changes. Updates Fusion display power feedback and starts or stops display use tracking.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `e` | `GenericDualEventArgs<string, bool>` | `Arg1` = the ID of the display that changed; `Arg2` = `true` if on, `false` if off. |

---

### AppServiceEndpointConnectionHandler(...)

```csharp
protected virtual void AppServiceEndpointConnectionHandler(object? sender, GenericDualEventArgs<string, bool> args)
```

Handle notifications about relay or control expander endpoint connection status changes.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `args` | `GenericDualEventArgs<string, bool>` | `Arg1` = the ID of the endpoint device that changed; `Arg2` = `true` if online, `false` if offline. |

---

### ApplicationCableTvConnectionHandler(...)

```csharp
protected virtual void ApplicationCableTvConnectionHandler(object? sender, GenericDualEventArgs<string, bool> args)
```

Sends offline/online state change notice for Cable TV devices that support it.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The application service that triggered the event. |
| `args` | `GenericDualEventArgs<string, bool>` | `Arg1` = the ID of the device that changed; `Arg2` = current state. |

---

### ApplicationBlurayConnectionHandler(...)

```csharp
protected virtual void ApplicationBlurayConnectionHandler(object? sender, GenericDualEventArgs<string, bool> args)
```

Sends offline/online state change notice for Blu-ray devices that support it.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The application service that triggered the event. |
| `args` | `GenericDualEventArgs<string, bool>` | `Arg1` = the ID of the device that changed; `Arg2` = current state. |

---

### AppServiceRouteHandler(...)

```csharp
protected virtual void AppServiceRouteHandler(object? sender, GenericSingleEventArgs<string> args)
```

Handle notifications about video routing events. Calls `UpdateFusionRoutingFeedback()`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `args` | `GenericSingleEventArgs<string>` | `args.Arg` = the ID of the destination that changed. |

---

### AppServiceRouterConnectionHandler(...)

```csharp
protected virtual void AppServiceRouterConnectionHandler(object? sender, GenericSingleEventArgs<string> args)
```

Handle notifications about AVR connection status changes. Clears or adds a Fusion offline error based on the new connection state.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `args` | `GenericSingleEventArgs<string>` | `args.Arg` = the ID of the AVR that updated. |

---

### AppServiceLightingConnectionHandler(...)

```csharp
protected virtual void AppServiceLightingConnectionHandler(object? sender, GenericDualEventArgs<string, bool> e)
```

Handle notifications about lighting controller connection status changes.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `e` | `GenericDualEventArgs<string, bool>` | `Arg1` = the ID of the lighting controller that updated; `Arg2` = `true` if online, `false` if offline. |

---

### AppServiceGlobalFreezeHandler(...)

```csharp
protected virtual void AppServiceGlobalFreezeHandler(object? sender, EventArgs e)
```

Handle notifications about global/AVR video freeze state changes. Sends the new freeze state to the Fusion server.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `e` | `EventArgs` | Generic empty event args. |

---

### AppServiceGlobalBlankHandler(...)

```csharp
protected virtual void AppServiceGlobalBlankHandler(object? sender, EventArgs e)
```

Handle notifications about global/AVR video blank state changes. Sends the new blank state to the Fusion server.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `e` | `EventArgs` | Generic empty event args. |

---

### AppServiceStateChangeHandler(...)

```csharp
protected virtual void AppServiceStateChangeHandler(object? sender, EventArgs args)
```

Handle notifications about system power state changes. Updates the Fusion system state. When powering off, stops all source use tracking; when powering on, calls `UpdateFusionRoutingFeedback()`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `args` | `EventArgs` | Generic empty event args. |

---

### UiConnectionHandler(...)

```csharp
protected virtual void UiConnectionHandler(object? sender, GenericSingleEventArgs<string> args)
```

Handle user interface connection changes. Updates the application service with the new connection status. When a non-XPanel UI goes offline, notifies all `IErrorInterface` and `IUiStatusMonitor` implementations.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `args` | `GenericSingleEventArgs<string>` | `args.Arg` = the ID of the user interface that changed. |

---

### FusionRouteSourceHandler(...)

```csharp
protected virtual void FusionRouteSourceHandler(object? sender, GenericSingleEventArgs<string> e)
```

Handle AV source route requests from the Fusion interface. Calls `AppService.RouteToAll()`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `e` | `GenericSingleEventArgs<string>` | `e.Arg` = the ID of the source to route to all video destinations. |

---

### FusionAudioLevelHandler(...)

```csharp
protected virtual void FusionAudioLevelHandler(object? sender, GenericSingleEventArgs<uint> e)
```

Handle program audio level change requests from the Fusion interface. Sets the level on the first output channel tagged `"pgm"`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `e` | `GenericSingleEventArgs<uint>` | `e.Arg` = the 0–100 level value to set. |

---

### FusionAudioMuteHandler(...)

```csharp
protected virtual void FusionAudioMuteHandler(object? sender, EventArgs e)
```

Handle program audio mute state change requests from the Fusion interface. Toggles the mute state on the first output channel tagged `"pgm"`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `e` | `EventArgs` | Empty args package. |

---

### FusionDisplayFreezeHandler(...)

```csharp
protected virtual void FusionDisplayFreezeHandler(object? sender, EventArgs e)
```

Handle global video freeze change requests from the Fusion interface. Toggles the current global freeze state.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `e` | `EventArgs` | Empty args package. |

---

### FusionDisplayBlankHandler(...)

```csharp
protected virtual void FusionDisplayBlankHandler(object? sender, EventArgs e)
```

Handle global video blank change requests from the Fusion interface. Toggles the current global blank state.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `e` | `EventArgs` | Empty args package. |

---

### FusionDisplayPowerHandler(...)

```csharp
protected virtual void FusionDisplayPowerHandler(object? sender, GenericSingleEventArgs<bool> e)
```

Handle display power change requests from the Fusion interface. Applies the requested power state to all displays.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `e` | `GenericSingleEventArgs<bool>` | `e.Arg` = `true` for on, `false` for off. |

---

### FusionPowerHandler(...)

```csharp
protected virtual void FusionPowerHandler(object? sender, GenericSingleEventArgs<bool> e)
```

Handle system use state change requests from the Fusion interface. Calls `AppService.SetActive()` or `AppService.SetStandby()`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `e` | `GenericSingleEventArgs<bool>` | `e.Arg` = `true` for set active, `false` for set standby. |

---

### FusionMicMuteHandler(...)

```csharp
protected virtual void FusionMicMuteHandler(object? sender, GenericSingleEventArgs<string> e)
```

Handle mic mute change requests from the Fusion interface. Toggles the mute state of the specified microphone.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `e` | `GenericSingleEventArgs<string>` | `e.Arg` = the ID of the microphone to toggle. |

---

### FusionConnectionHandler(...)

```csharp
protected virtual void FusionConnectionHandler(object? sender, EventArgs e)
```

Handle Fusion interface device online/offline state change events. Calls `UpdateFusionFeedback()` when the Fusion server comes online.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sender` | `object?` | The object that triggered the event. |
| `e` | `EventArgs` | Empty args package. |

---

### UpdateFusionDisplayPowerFeedback()

```csharp
protected virtual void UpdateFusionDisplayPowerFeedback()
```

Update the Fusion server with the current display power state. Sends `true` if any display is currently powered on.

---

### UpdateFusionAudioFeedback()

```csharp
protected virtual void UpdateFusionAudioFeedback()
```

Update the Fusion server with the current state of program audio level and mute, using the first output channel tagged `"pgm"`.

---

### UpdateFusionRoutingFeedback()

```csharp
protected virtual void UpdateFusionRoutingFeedback()
```

Update the Fusion server with the current video route status using the first AV destination in the configuration.

---

### UpdateFusionFeedback()

```csharp
protected virtual void UpdateFusionFeedback()
```

Update the Fusion server with all supported feedback: system state, display power, audio level/mute, routing, video freeze, and video blank.
