# FusionInterface

**Namespace:** `gcu_ui_service.Fusion`

**Implements:** [`IFusionInterface`](IFusionInterface.md)

Concrete implementation of [`IFusionInterface`](IFusionInterface.md). Manages a Crestron `FusionRoom` connection, routes Fusion server events to the appropriate AVF event handlers, and delegates device use tracking and error management to internal `FusionDeviceUse` and `FusionErrorManager` objects. Implements `IDisposable` to cleanly unregister Fusion signals and event handlers.

---

## Table of Contents

**Constructors**
- [FusionInterface(uint ipId, CrestronControlSystem control, string name, string guid)](#fusioninterfaceuint-ipid-crestroncontrolsystem-control-string-name-string-guid)

**Events**
- [OnlineStatusChanged](#onlinestatuschanged)
- [SystemStateChangeRequested](#systemstatechangerequested)
- [DisplayPowerChangeRequested](#displaypowerchangerequested)
- [AudioMuteChangeRequested](#audiomuntchangerequested)
- [DisplayBlankChangeRequested](#displayblankchangerequested)
- [DisplayFreezeChangeRequested](#displayfreezechangerequested)
- [ProgramAudioChangeRequested](#programaudiochangerequested)
- [MicMuteChangeRequested](#micmutechangerequested)
- [SourceSelectRequested](#sourceselectrequested)

**Properties**
- [IsOnline](#isonline)

**Methods**
- [Initialize()](#initialize)
- [UpdateSystemState(bool state)](#updatesystemstatebool-state)
- [UpdateProgramAudioMute(bool state)](#updateprogramaudiomutebool-state)
- [UpdateProgramAudioLevel(uint level)](#updateprogramaudioleveluint-level)
- [UpdateDisplayPower(bool state)](#updatedisplaypowerbool-state)
- [UpdateDisplayBlank(bool state)](#updatedisplayblankbool-state)
- [UpdateDisplayFreeze(bool state)](#updatedisplayfreezebool-state)
- [AddMicrophone(string id, string label, string[] tags)](#addmicrophonestring-id-string-label-string-tags)
- [UpdateMicMute(string id, bool state)](#updatemicmutestring-id-bool-state)
- [AddAvSource(string id, string label, string[] tags)](#addavsourcestring-id-string-label-string-tags)
- [UpdateSelectedSource(string id)](#updateselectedsourcestring-id)
- [AddDeviceToUseTracking(string id, string label)](#adddevicetousetrackinstring-id-string-label)
- [StartDeviceUse(string id)](#startdeviceusestring-id)
- [StopDeviceUse(string id)](#stopdeviceusestring-id)
- [AddDisplayToUseTracking(string id, string label)](#adddisplaytousetrackinstring-id-string-label)
- [StartDisplayUse(string id)](#startdisplayusestring-id)
- [StopDisplayUse(string id)](#stopdisplayusestring-id)
- [AddOfflineDevice(string devId, string message)](#addofflinedevicestring-devid-string-message)
- [ClearOfflineDevice(string devId)](#clearofflinedevicestring-devid)
- [Dispose()](#dispose)

---

## Constructors

### FusionInterface(uint ipId, CrestronControlSystem control, string name, string guid)

```csharp
public FusionInterface(uint ipId, CrestronControlSystem control, string name, string guid)
```

Instantiates a new instance of `FusionInterface` and creates the internal `FusionRoom`, `FusionDeviceUse`, and `FusionErrorManager` objects.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `ipId` | `uint` | The Crestron IP-ID used to register the Fusion room device. |
| `control` | `CrestronControlSystem` | The root Crestron control system object. |
| `name` | `string` | The name of the Fusion room as configured on the Fusion server. |
| `guid` | `string` | The globally unique identifier for this Fusion room instance. |

---

## Events

See [`IFusionInterface`](IFusionInterface.md) for full event descriptions.

| Event | Signature |
|-------|-----------|
| `OnlineStatusChanged` | `EventHandler<EventArgs>?` |
| `SystemStateChangeRequested` | `EventHandler<GenericSingleEventArgs<bool>>?` |
| `DisplayPowerChangeRequested` | `EventHandler<GenericSingleEventArgs<bool>>?` |
| `AudioMuteChangeRequested` | `EventHandler<EventArgs>?` |
| `DisplayBlankChangeRequested` | `EventHandler<EventArgs>?` |
| `DisplayFreezeChangeRequested` | `EventHandler<EventArgs>?` |
| `ProgramAudioChangeRequested` | `EventHandler<GenericSingleEventArgs<uint>>?` |
| `MicMuteChangeRequested` | `EventHandler<GenericSingleEventArgs<string>>?` |
| `SourceSelectRequested` | `EventHandler<GenericSingleEventArgs<string>>?` |

---

## Properties

### IsOnline

```csharp
public bool IsOnline { get; }
```

Gets a value indicating whether the system is connected to the Fusion server. Delegates to the underlying `FusionRoom.IsOnline`.

---

## Methods

### Initialize()

```csharp
public void Initialize()
```

Registers all custom Fusion signals (video mute, video freeze, source select inputs, audio level, lamp hours, mic mute, and podium mute), subscribes to Fusion server state change events, generates the RVI file, and registers the `FusionRoom` device with the Crestron control system.

---

### UpdateSystemState(bool state)

```csharp
public void UpdateSystemState(bool state)
```

Send a notification to the Fusion server that the system use state has changed. See [`IFusionInterface.UpdateSystemState`](IFusionInterface.md#updatesystemstatebool-state).

---

### UpdateProgramAudioMute(bool state)

```csharp
public void UpdateProgramAudioMute(bool state)
```

Send a notification to the Fusion server that the program audio mute status has changed. See [`IFusionInterface.UpdateProgramAudioMute`](IFusionInterface.md#updateprogramaudiomutebool-state).

---

### UpdateProgramAudioLevel(uint level)

```csharp
public void UpdateProgramAudioLevel(uint level)
```

Send a notification to the Fusion server that the program audio level has changed. See [`IFusionInterface.UpdateProgramAudioLevel`](IFusionInterface.md#updateprogramaudioleveluint-level).

---

### UpdateDisplayPower(bool state)

```csharp
public void UpdateDisplayPower(bool state)
```

Send a notification to the Fusion server that there is a display powered on. See [`IFusionInterface.UpdateDisplayPower`](IFusionInterface.md#updatedisplaypowerbool-state).

---

### UpdateDisplayBlank(bool state)

```csharp
public void UpdateDisplayBlank(bool state)
```

Send a notification to the Fusion server that the global video blank status has changed. See [`IFusionInterface.UpdateDisplayBlank`](IFusionInterface.md#updatedisplayblankbool-state).

---

### UpdateDisplayFreeze(bool state)

```csharp
public void UpdateDisplayFreeze(bool state)
```

Send a notification to the Fusion server that the global video freeze state has changed. See [`IFusionInterface.UpdateDisplayFreeze`](IFusionInterface.md#updatedisplayfreezebool-state).

---

### AddMicrophone(string id, string label, string[] tags)

```csharp
public void AddMicrophone(string id, string label, string[] tags)
```

Add a microphone to the internal tracker. See [`IFusionInterface.AddMicrophone`](IFusionInterface.md#addmicrophonestring-id-string-label-string-tags).

---

### UpdateMicMute(string id, bool state)

```csharp
public void UpdateMicMute(string id, bool state)
```

Send a notification to the Fusion server that the mute state has changed for a microphone. See [`IFusionInterface.UpdateMicMute`](IFusionInterface.md#updatemicmutestring-id-bool-state).

---

### AddAvSource(string id, string label, string[] tags)

```csharp
public void AddAvSource(string id, string label, string[] tags)
```

Add an AV source to the internal collection of sources selectable from the Fusion server dashboard. See [`IFusionInterface.AddAvSource`](IFusionInterface.md#addavsourcestring-id-string-label-string-tags).

---

### UpdateSelectedSource(string id)

```csharp
public void UpdateSelectedSource(string id)
```

Send a notification to the Fusion server that the currently selected AV source has changed. See [`IFusionInterface.UpdateSelectedSource`](IFusionInterface.md#updateselectedsourcestring-id).

---

### AddDeviceToUseTracking(string id, string label)

```csharp
public void AddDeviceToUseTracking(string id, string label)
```

Delegates to the internal `FusionDeviceUse` instance. See [`IFusionDeviceUse.AddDeviceToUseTracking`](IFusionDeviceUse.md#adddevicetousetrackinstring-id-string-label).

---

### StartDeviceUse(string id)

```csharp
public void StartDeviceUse(string id)
```

Delegates to the internal `FusionDeviceUse` instance. See [`IFusionDeviceUse.StartDeviceUse`](IFusionDeviceUse.md#startdeviceusestring-id).

---

### StopDeviceUse(string id)

```csharp
public void StopDeviceUse(string id)
```

Delegates to the internal `FusionDeviceUse` instance. See [`IFusionDeviceUse.StopDeviceUse`](IFusionDeviceUse.md#stopdeviceusestring-id).

---

### AddDisplayToUseTracking(string id, string label)

```csharp
public void AddDisplayToUseTracking(string id, string label)
```

Delegates to the internal `FusionDeviceUse` instance. See [`IFusionDeviceUse.AddDisplayToUseTracking`](IFusionDeviceUse.md#adddisplaytousetrackinstring-id-string-label).

---

### StartDisplayUse(string id)

```csharp
public void StartDisplayUse(string id)
```

Delegates to the internal `FusionDeviceUse` instance. See [`IFusionDeviceUse.StartDisplayUse`](IFusionDeviceUse.md#startdisplayusestring-id).

---

### StopDisplayUse(string id)

```csharp
public void StopDisplayUse(string id)
```

Delegates to the internal `FusionDeviceUse` instance. See [`IFusionDeviceUse.StopDisplayUse`](IFusionDeviceUse.md#stopdisplayusestring-id).

---

### AddOfflineDevice(string devId, string message)

```csharp
public void AddOfflineDevice(string devId, string message)
```

Delegates to the internal `FusionErrorManager` instance. See [`IFusionErrorManager.AddOfflineDevice`](IFusionErrorManager.md#addofflinedevicestring-devid-string-message).

---

### ClearOfflineDevice(string devId)

```csharp
public void ClearOfflineDevice(string devId)
```

Delegates to the internal `FusionErrorManager` instance. See [`IFusionErrorManager.ClearOfflineDevice`](IFusionErrorManager.md#clearofflinedevicestring-devid).

---

### Dispose()

```csharp
public void Dispose()
```

Unsubscribes from Fusion server events, removes all registered custom signals, unregisters the `FusionRoom` device, and releases managed resources.
