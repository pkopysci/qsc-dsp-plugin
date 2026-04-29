# ApplicationService

**Namespace:** `gcu_application_service`

**Implements:** [`IApplicationService`](IApplicationService.md), [`IAvIpMatrixApp`](IAvIpMatrixApp.md), [`IAudioRedundancyApp`](IAudioRedundancyApp.md), [`IAudioPresetApp`](IAudioPresetApp.md), `IDisposable`

Default implementation of `IApplicationService`. Controls interactions between external control interfaces and the hardware service. Plugin authors can extend this class to override specific behaviors without reimplementing the full interface.

---

## Table of Contents

**Protected Fields**
- [Disposables](#disposables)
- [InterfaceData](#interfacedata)
- [DisplayControl](#displaycontrol)
- [SystemPowerControl](#systempowercontrol)
- [EndpointControl](#endpointcontrol)
- [HwService](#hwservice)
- [Domain](#domain)
- [AudioControl](#audiocontrol)
- [RoutingControl](#routingcontrol)
- [TransportControl](#transportcontrol)
- [LightingControl](#lightingcontrol)
- [UseAvrMuteFreeze](#useavrmuttefreeze)

**Public Methods**
- [Initialize(IInfrastructureService hwService, IDomainService domain)](#initializeiinfrastructureservice-hwservice-idomainservice-domain)
- [Dispose()](#dispose)
- [NotifySystemStateChanged()](#notifysystemstatechanged)

**Protected Methods**
- [Dispose(bool disposing)](#disposebool-disposing)
- [HandleStartupShutdownPresets()](#handlestartupshutdownpresets)
- [HandleLightingStartupShutdown()](#handlelightingstartupsutdown)
- [OnSystemChange()](#onsystemchange)
- [SubscribeEvents()](#subscribeevents)
- [SubscribeToDisplayEvents()](#subscribetodisplayevents)
- [SubscribeToEndpointEvents()](#subscribetoendpointevents)
- [SubscribeToTransportEvents()](#subscribetotransportevents)
- [SubscribeToAudioEvents()](#subscribetoa-audioevents)
- [SubscribeToVideoEvents()](#subscribetovideo-events)
- [SubscribeToLightingEvents()](#subscribetolightingevents)
- [OnAudioRouteChange(object? sender, GenericSingleEventArgs\<string\> args)](#onaudioroutechange)
- [OnRoutingControlRouteChange(object? sender, GenericSingleEventArgs\<string\> args)](#onroutingcontrolroutechange)
- [OnAvIpEndpointStatusChanged(object? sender, GenericDualEventArgs\<string, string\> args)](#onavipendpointstatuschanged)
- [SetAvrVideoFreeze(bool state)](#setavrvideofreezebbool-state)
- [SetAvrVideoBlank(bool state)](#setavrvideoblankbool-state)

All `IApplicationService`, `IAvIpMatrixApp`, `IAudioRedundancyApp`, and `IAudioPresetApp` members are also implemented — see their respective interface documentation for signatures.

---

## Protected Fields

### Disposables

```csharp
protected readonly List<IDisposable> Disposables
```

Collection of objects that will be disposed when this instance is disposed.

---

### InterfaceData

```csharp
protected List<UserInterfaceDataContainer> InterfaceData
```

Collection of all user interfaces defined in the system configuration.

---

### DisplayControl

```csharp
protected IDisplayControlApp DisplayControl
```

Internal control object for managing display control state.

---

### SystemPowerControl

```csharp
protected ISystemPowerApp SystemPowerControl
```

Internal control object for managing system power state and requests.

---

### EndpointControl

```csharp
protected IEndpointControlApp EndpointControl
```

Internal control object for managing relay endpoint control requests.

---

### HwService

```csharp
protected IInfrastructureService HwService
```

The hardware device interface manager (gcu-hardware-service).

---

### Domain

```csharp
protected IDomainService Domain
```

The system configuration representation (gcu-domain-service).

---

### AudioControl

```csharp
protected IAudioControlApp AudioControl
```

Internal control object for managing audio state and requests.

---

### RoutingControl

```csharp
protected IAvRoutingApp RoutingControl
```

Internal control object for managing video routing state and requests.

---

### TransportControl

```csharp
protected ITransportControlApp TransportControl
```

Internal control object for managing Blu-ray, cable TV, and other transport-based requests.

---

### LightingControl

```csharp
protected ILightingControlApp LightingControl
```

Internal control object for managing all lighting states and requests.

---

### UseAvrMuteFreeze

```csharp
protected bool UseAvrMuteFreeze
```

When `true`, global video mute and freeze commands are sent to the AV router hardware. When `false`, they are sent to individual displays. Set automatically during `Initialize()` based on whether any AV switcher implements `IVideoBlankDevice`.

---

## Public Methods

### Initialize(IInfrastructureService hwService, IDomainService domain)

```csharp
public virtual void Initialize(IInfrastructureService hwService, IDomainService domain)
```

Creates all internal control objects (system power, display, endpoint, audio, routing, transport, lighting) and subscribes to their events.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `hwService` | `IInfrastructureService` | The hardware control service. |
| `domain` | `IDomainService` | The system configuration domain. |

---

### Dispose()

```csharp
public void Dispose()
```

Disposes all objects in the `Disposables` collection and releases managed resources.

---

### NotifySystemStateChanged()

```csharp
protected void NotifySystemStateChanged()
```

Allows child classes to notify subscribers of a system state change. Use this when overriding the core state management logic of `SystemPowerControl`.

---

## Protected Methods

### Dispose(bool disposing)

```csharp
protected virtual void Dispose(bool disposing)
```

Disposes all managed objects in `Disposables`.

---

### HandleStartupShutdownPresets()

```csharp
protected virtual void HandleStartupShutdownPresets()
```

Recalls any startup or shutdown DSP presets defined in the system configuration. Looks for a preset with ID `"STARTUP"` or `"SHUTDOWN"` on each DSP.

---

### HandleLightingStartupShutdown()

```csharp
protected virtual void HandleLightingStartupShutdown()
```

Triggers startup or shutdown lighting scenes if they are defined in the system configuration.

---

### OnSystemChange()

```csharp
protected virtual void OnSystemChange()
```

Triggers AV routes flagged for startup or shutdown events, then calls `HandleStartupShutdownPresets()` and `HandleLightingStartupShutdown()`.

---

### SubscribeEvents()

```csharp
protected virtual void SubscribeEvents()
```

Subscribes to all events from internal subsystem control objects. Called at the end of `Initialize()`.

---

### SubscribeToDisplayEvents()

```csharp
protected virtual void SubscribeToDisplayEvents()
```

Subscribe to all display control events and forward them to the corresponding public events.

---

### SubscribeToEndpointEvents()

```csharp
protected virtual void SubscribeToEndpointEvents()
```

Subscribe to all IO endpoint change events and forward them to the corresponding public events.

---

### SubscribeToTransportEvents()

```csharp
protected virtual void SubscribeToTransportEvents()
```

Subscribes to all Blu-ray and cable TV connection events.

---

### SubscribeToAudioEvents()

```csharp
protected virtual void SubscribeToAudioEvents()
```

Subscribes to all audio control related events.

---

### SubscribeToVideoEvents()

```csharp
protected virtual void SubscribeToVideoEvents()
```

Subscribes to all video routing control events. Also sets `UseAvrMuteFreeze` if any AV switcher implements `IVideoBlankDevice`.

---

### SubscribeToLightingEvents()

```csharp
protected virtual void SubscribeToLightingEvents()
```

Subscribes to all lighting control events.

---

### OnAudioRouteChange(...)

```csharp
protected virtual void OnAudioRouteChange(object? sender, GenericSingleEventArgs<string> args)
```

Handler for when the audio control component reports a route change. `args.Arg` is the ID of the destination that changed.

---

### OnRoutingControlRouteChange(...)

```csharp
protected virtual void OnRoutingControlRouteChange(object? sender, GenericSingleEventArgs<string> args)
```

Handler for when the routing control component reports a route change. `args.Arg` is the ID of the destination that changed.

---

### OnAvIpEndpointStatusChanged(...)

```csharp
protected virtual void OnAvIpEndpointStatusChanged(object? sender, GenericDualEventArgs<string, string> args)
```

Captures AV-over-IP endpoint connection changes and broadcasts the `AvIpEndpointConnectionChanged` event. `Args.Arg1` is the AVR ID; `Args.Arg2` is the endpoint ID.

---

### SetAvrVideoFreeze(bool state)

```csharp
protected virtual void SetAvrVideoFreeze(bool state)
```

Iterate through all AV switchers and set their video freeze state, if supported.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `state` | `bool` | `true` = freeze active; `false` = freeze inactive. |

---

### SetAvrVideoBlank(bool state)

```csharp
protected virtual void SetAvrVideoBlank(bool state)
```

Iterate through all AV switchers and set their video blank state, if supported.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `state` | `bool` | `true` = blank active; `false` = blank inactive. |
