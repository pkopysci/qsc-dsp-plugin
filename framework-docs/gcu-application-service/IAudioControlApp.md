# IAudioControlApp

**Namespace:** `gcu_application_service.AudioControl`

Common properties and methods for audio device control management.

---

## Table of Contents

**Events**
- [AudioOutputLevelChanged](#audiooutputlevelchanged)
- [AudioOutputMuteChanged](#audiooutputmutechanged)
- [AudioInputLevelChanged](#audioinputlevelchanged)
- [AudioInputMuteChanged](#audioinputmutechanged)
- [AudioDspConnectionStatusChanged](#audiodspconnectionstatuschanged)
- [AudioOutputRouteChanged](#audiooutputroutechanged)
- [AudioZoneEnableChanged](#audiozoneeenablechanged)

**Methods**
- [GetAudioInputChannels()](#getaudioinputchannels)
- [GetAudioOutputChannels()](#getaudiooutputchannels)
- [GetAllAudioDspDevices()](#getallaudiodspdevices)
- [QueryAudioDspConnectionStatus(string id)](#queryaudiodspconnectionstatusstring-id)
- [QueryAudioInputLevel(string id)](#queryaudioinputlevelstring-id)
- [QueryAudioOutputLevel(string id)](#queryaudiooutputlevelstring-id)
- [QueryAudioOutputMute(string id)](#queryaudiooutputmutestring-id)
- [QueryAudioOutputRoute(string id)](#queryaudiooutputroutestring-id)
- [QueryAudioInputMute(string id)](#queryaudioinputmutestring-id)
- [QueryAudioZoneState(string channelId, string zoneId)](#queryaudiozonestatestring-channelid-string-zoneid)
- [SetAudioInputLevel(string id, int level)](#setaudioinputlevelstring-id-int-level)
- [SetAudioInputMute(string id, bool mute)](#setaudioinputmutestring-id-bool-mute)
- [SetAudioOutputLevel(string id, int level)](#setaudiooutputlevelstring-id-int-level)
- [SetAudioOutputMute(string id, bool mute)](#setaudiooutputmutestring-id-bool-mute)
- [SetAudioOutputRoute(string srcId, string destId)](#setaudiooutputroutestring-srcid-string-destid)
- [ToggleAudioZoneState(string channelId, string zoneId)](#toggleaudiozonestatestring-channelid-string-zoneid)
- [SetAudioZoneState(string channelId, string zoneId, bool state)](#setaudiozonestatestring-channelid-string-zoneid-bool-state)

---

## Events

### AudioOutputLevelChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>>? AudioOutputLevelChanged
```

Triggered when the audio monitor detects a change on an output channel level. The event arg is the output channel ID.

---

### AudioOutputMuteChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>>? AudioOutputMuteChanged
```

Triggered when the audio monitor detects a change on an output channel mute status. The event arg is the output channel ID.

---

### AudioInputLevelChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>>? AudioInputLevelChanged
```

Triggered when the audio monitor detects a change on an input channel level. The event arg is the input channel ID.

---

### AudioInputMuteChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>>? AudioInputMuteChanged
```

Triggered when the audio monitor detects a change on an input channel mute status. The event arg is the input channel ID.

---

### AudioDspConnectionStatusChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>>? AudioDspConnectionStatusChanged
```

Triggered whenever the connection to a DSP changes. The event arg is the DSP device ID.

---

### AudioOutputRouteChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>>? AudioOutputRouteChanged
```

Triggered whenever an audio-routable DSP reports a route change on an output channel. The event arg is the output channel ID.

---

### AudioZoneEnableChanged

```csharp
event EventHandler<GenericDualEventArgs<string, string>>? AudioZoneEnableChanged
```

Triggered whenever the system detects an audio zone enable/disable event. `Arg1` is the channel ID that changed; `Arg2` is the zone ID that changed.

---

## Methods

### GetAudioInputChannels()

```csharp
ReadOnlyCollection<AudioChannelInfoContainer> GetAudioInputChannels()
```

Get all input channels defined in the system configuration.

**Returns:** All AV input channels in the system design.

---

### GetAudioOutputChannels()

```csharp
ReadOnlyCollection<AudioChannelInfoContainer> GetAudioOutputChannels()
```

Get all output channels defined in the system configuration.

**Returns:** All AV output channels in the system design.

---

### GetAllAudioDspDevices()

```csharp
ReadOnlyCollection<DspInfoContainer> GetAllAudioDspDevices()
```

Gets information about all audio DSP devices in the configuration. Returns an empty collection if there are none.

**Returns:** A collection of information on all DSP devices in the system.

---

### QueryAudioDspConnectionStatus(string id)

```csharp
bool QueryAudioDspConnectionStatus(string id)
```

Get the current connection status of the target DSP.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the DSP to query. |

**Returns:** `true` if the device exists and is online; `false` otherwise.

---

### QueryAudioInputLevel(string id)

```csharp
int QueryAudioInputLevel(string id)
```

Get the current level of the target input channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the input to query. |

**Returns:** A value from 0–100 representing the current volume level.

---

### QueryAudioOutputLevel(string id)

```csharp
int QueryAudioOutputLevel(string id)
```

Get the current level of the target output channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the output to query. |

**Returns:** A value from 0–100 representing the current volume level.

---

### QueryAudioOutputMute(string id)

```csharp
bool QueryAudioOutputMute(string id)
```

Get the current mute status of the target output.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the output to query. |

**Returns:** `true` if the output exists and is muted; `false` otherwise.

---

### QueryAudioOutputRoute(string id)

```csharp
string QueryAudioOutputRoute(string id)
```

Get the current route status of the target output channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the output channel to query. |

**Returns:** The unique ID of the input channel currently routed to that output.

---

### QueryAudioInputMute(string id)

```csharp
bool QueryAudioInputMute(string id)
```

Get the current mute status of the target input.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the input to query. |

**Returns:** `true` if the input exists and is muted; `false` otherwise.

---

### QueryAudioZoneState(string channelId, string zoneId)

```csharp
bool QueryAudioZoneState(string channelId, string zoneId)
```

Get the current state of the target zone enable control for the given channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `channelId` | `string` | The unique ID of the audio channel to query. |
| `zoneId` | `string` | The unique ID of the zone control for the channel to query. |

**Returns:** The current state of the zone enable, or `false` if no channel/zone pair was found.

---

### SetAudioInputLevel(string id, int level)

```csharp
void SetAudioInputLevel(string id, int level)
```

Send a request to change the audio level of an input channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the input channel to adjust. |
| `level` | `int` | A value from 0–100 representing the new level. |

---

### SetAudioInputMute(string id, bool mute)

```csharp
void SetAudioInputMute(string id, bool mute)
```

Send a request to change the mute state of an input channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the input channel to update. |
| `mute` | `bool` | `true` = enable mute (no audio); `false` = disable mute (pass audio). |

---

### SetAudioOutputLevel(string id, int level)

```csharp
void SetAudioOutputLevel(string id, int level)
```

Send a request to change the audio level of an output channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the output channel to adjust. |
| `level` | `int` | A value from 0–100 representing the new level. |

---

### SetAudioOutputMute(string id, bool mute)

```csharp
void SetAudioOutputMute(string id, bool mute)
```

Send a request to change the mute state of an output channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the output channel to update. |
| `mute` | `bool` | `true` = enable mute (no audio); `false` = disable mute (pass audio). |

---

### SetAudioOutputRoute(string srcId, string destId)

```csharp
void SetAudioOutputRoute(string srcId, string destId)
```

Set the audio route on the target output channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `srcId` | `string` | The unique ID of the input channel to route. |
| `destId` | `string` | The unique ID of the output channel to update. |

---

### ToggleAudioZoneState(string channelId, string zoneId)

```csharp
void ToggleAudioZoneState(string channelId, string zoneId)
```

Toggle the current enable state of the target channel/zone combination. Does nothing if a matching channel ID/zone ID pair cannot be found.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `channelId` | `string` | The unique ID of the audio channel to modify. |
| `zoneId` | `string` | The unique ID of the zone toggle to change. |

---

### SetAudioZoneState(string channelId, string zoneId, bool state)

```csharp
void SetAudioZoneState(string channelId, string zoneId, bool state)
```

Discretely set an output zone mix state.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `channelId` | `string` | The unique ID of the audio channel to modify. |
| `zoneId` | `string` | The unique ID of the zone toggle to change. |
| `state` | `bool` | `true` = enable the channel mix; `false` = disable/mute the channel mix. |
