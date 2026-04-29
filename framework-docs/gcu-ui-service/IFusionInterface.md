# IFusionInterface

**Namespace:** `gcu_ui_service.Fusion`

**Implements:** [`IFusionDeviceUse`](IFusionDeviceUse.md), [`IFusionErrorManager`](IFusionErrorManager.md), `IDisposable`

Required methods, properties, and events for supporting a Fusion connection in the AVF. Aggregates device use tracking, error management, and bidirectional communication with a Crestron Fusion server. Supports AV source selection, display power, audio level/mute, video blank/freeze, and microphone mute control.

---

## Table of Contents

**Events**
- [OnlineStatusChanged](#onlinestatuschanged)
- [SystemStateChangeRequested](#systemstatechangerequested)
- [DisplayPowerChangeRequested](#displaypowerchangerequested)
- [AudioMuteChangeRequested](#audiomunechangerequested)
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

---

## Events

### OnlineStatusChanged

```csharp
event EventHandler<EventArgs>? OnlineStatusChanged
```

Triggered when the Fusion server connection comes online or goes offline.

---

### SystemStateChangeRequested

```csharp
event EventHandler<GenericSingleEventArgs<bool>>? SystemStateChangeRequested
```

Triggered whenever a system power event is received from the Fusion server. The event arg is `true` to set active, `false` to set standby.

---

### DisplayPowerChangeRequested

```csharp
event EventHandler<GenericSingleEventArgs<bool>>? DisplayPowerChangeRequested
```

Triggered whenever a display power event is received from the Fusion server. The event arg is `true` to power on, `false` to power off.

---

### AudioMuteChangeRequested

```csharp
event EventHandler<EventArgs>? AudioMuteChangeRequested
```

Triggered whenever a program audio mute event is received from the Fusion server.

---

### DisplayBlankChangeRequested

```csharp
event EventHandler<EventArgs>? DisplayBlankChangeRequested
```

Triggered whenever a "blank all displays" event is received from the Fusion server.

---

### DisplayFreezeChangeRequested

```csharp
event EventHandler<EventArgs>? DisplayFreezeChangeRequested
```

Triggered whenever a "freeze all displays" event is received from the Fusion server.

---

### ProgramAudioChangeRequested

```csharp
event EventHandler<GenericSingleEventArgs<uint>>? ProgramAudioChangeRequested
```

Triggered whenever a request to change the program audio level is received from the server. The event arg contains the 0–100 value that the level should be set to.

---

### MicMuteChangeRequested

```csharp
event EventHandler<GenericSingleEventArgs<string>>? MicMuteChangeRequested
```

Triggered whenever a request to change the mute state of a microphone is received from the Fusion server. The event arg contains the ID of the microphone to toggle.

---

### SourceSelectRequested

```csharp
event EventHandler<GenericSingleEventArgs<string>>? SourceSelectRequested
```

Triggered whenever a request to change the selected AV input is received from the Fusion server. The event arg contains the ID of the input source to select.

---

## Properties

### IsOnline

```csharp
bool IsOnline { get; }
```

Gets a value indicating whether the system is connected to the Fusion server.

---

## Methods

### Initialize()

```csharp
void Initialize()
```

Registers internal Fusion server objects and attempts to establish a connection.

---

### UpdateSystemState(bool state)

```csharp
void UpdateSystemState(bool state)
```

Send a notification to the Fusion server that the system use state has changed (active or standby).

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `state` | `bool` | `true` = system is active, `false` = system is in standby. |

---

### UpdateProgramAudioMute(bool state)

```csharp
void UpdateProgramAudioMute(bool state)
```

Send a notification to the Fusion server that the program audio mute status has changed.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `state` | `bool` | `true` = program audio is muted, `false` = program audio is not muted. |

---

### UpdateProgramAudioLevel(uint level)

```csharp
void UpdateProgramAudioLevel(uint level)
```

Send a notification to the Fusion server that the program audio level has changed.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `level` | `uint` | The 0–100 value representing the new audio level. |

---

### UpdateDisplayPower(bool state)

```csharp
void UpdateDisplayPower(bool state)
```

Send a notification to the Fusion server that there is a display powered on.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `state` | `bool` | `true` = a display is on, `false` = a display is off. |

---

### UpdateDisplayBlank(bool state)

```csharp
void UpdateDisplayBlank(bool state)
```

Send a notification to the Fusion server that the global video blank status has changed.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `state` | `bool` | `true` = video is blanked, `false` = video is active. |

---

### UpdateDisplayFreeze(bool state)

```csharp
void UpdateDisplayFreeze(bool state)
```

Send a notification to the Fusion server that the global video freeze state has changed.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `state` | `bool` | `true` = video is frozen, `false` = video is not frozen. |

---

### AddMicrophone(string id, string label, string[] tags)

```csharp
void AddMicrophone(string id, string label, string[] tags)
```

Add a microphone to the internal tracker used when requesting mic mute changes. The tags collection is searched for the keyword `"podium"` to assign the mic to the podium mute trigger. Any other mic is assigned to the generic mic mute trigger. If multiple mics are added to either trigger, the last one added will be sent on the event.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the mic to add. |
| `label` | `string` | The user-friendly name of the microphone. |
| `tags` | `string[]` | Collection of functionality tags. Searched for the keyword `"podium"`. |

---

### UpdateMicMute(string id, bool state)

```csharp
void UpdateMicMute(string id, bool state)
```

Send a notification to the Fusion server that the mute state has changed for a microphone in the system.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the microphone that changed. |
| `state` | `bool` | `true` = mic is muted, `false` = mic is passing audio. |

---

### AddAvSource(string id, string label, string[] tags)

```csharp
void AddAvSource(string id, string label, string[] tags)
```

Add an AV source to the internal collection of sources selectable from the Fusion server dashboard. The tags collection is searched for the keywords `"vga"`, `"hdmi"`, and `"pc"` to assign the source to the appropriate Fusion input trigger.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the source. Used when sending change events. |
| `label` | `string` | The user-friendly name or label of the AV source. |
| `tags` | `string[]` | Collection of functionality tags. Searched for `"vga"`, `"hdmi"`, and `"pc"`. |

---

### UpdateSelectedSource(string id)

```csharp
void UpdateSelectedSource(string id)
```

Send a notification to the Fusion server that the currently selected AV source has changed.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the source that is currently routed. |
