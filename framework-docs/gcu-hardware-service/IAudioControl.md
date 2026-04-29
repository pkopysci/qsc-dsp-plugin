# IAudioControl

**Namespace:** `gcu_hardware_service.AudioDevices`

**Implements:** [`IBaseDevice`](IBaseDevice.md)

Common properties and methods for basic audio level control. All DSP device plugins must implement this interface.

---

## Table of Contents

**Events**
- [AudioInputLevelChanged](#audioinputlevelchanged)
- [AudioInputMuteChanged](#audioinputmutechanged)
- [AudioOutputLevelChanged](#audiooutputlevelchanged)
- [AudioOutputMuteChanged](#audiooutputmutechanged)

**Methods**
- [GetAudioPresetIds()](#getaudiopresetids)
- [GetAudioInputIds()](#getaudioinputids)
- [GetAudioOutputIds()](#getaudiooutputids)
- [SetAudioInputLevel(string id, int level)](#setaudioinputlevelstring-id-int-level)
- [GetAudioInputLevel(string id)](#getaudioinputlevelstring-id)
- [SetAudioInputMute(string id, bool mute)](#setaudioinputmutestring-id-bool-mute)
- [GetAudioInputMute(string id)](#getaudioinputmutestring-id)
- [SetAudioOutputLevel(string id, int level)](#setaudiooutputlevelstring-id-int-level)
- [GetAudioOutputLevel(string id)](#getaudiooutputlevelstring-id)
- [SetAudioOutputMute(string id, bool mute)](#setaudiooutputmutestring-id-bool-mute)
- [GetAudioOutputMute(string id)](#getaudiooutputmutestring-id)
- [RecallAudioPreset(string id)](#recallaudiopresetstring-id)
- [AddInputChannel(...)](#addinputchannel)
- [AddOutputChannel(...)](#addoutputchannel)
- [AddPreset(string id, string bank, int index)](#addpresetstring-id-string-bank-int-index)

---

## Events

### AudioInputLevelChanged

```csharp
event EventHandler<GenericDualEventArgs<string, string>> AudioInputLevelChanged
```

Triggered when a volume/level change is detected on any input audio channel. Event args: arg1 = DSP ID, arg2 = channel ID.

---

### AudioInputMuteChanged

```csharp
event EventHandler<GenericDualEventArgs<string, string>> AudioInputMuteChanged
```

Triggered when a mute state change is detected on any input audio channel. Event args: arg1 = DSP ID, arg2 = channel ID.

---

### AudioOutputLevelChanged

```csharp
event EventHandler<GenericDualEventArgs<string, string>> AudioOutputLevelChanged
```

Triggered when a volume/level change is detected on any output audio channel. Event args: arg1 = DSP ID, arg2 = channel ID.

---

### AudioOutputMuteChanged

```csharp
event EventHandler<GenericDualEventArgs<string, string>> AudioOutputMuteChanged
```

Triggered when a mute state change is detected on any output audio channel. Event args: arg1 = DSP ID, arg2 = channel ID.

---

## Methods

### GetAudioPresetIds()

```csharp
IEnumerable<string> GetAudioPresetIds()
```

Gets the IDs of all the presets that were added to this device when created.

**Returns:** An enumerable of preset ID strings.

---

### GetAudioInputIds()

```csharp
IEnumerable<string> GetAudioInputIds()
```

Gets the IDs of all the input channels added to this device when created.

**Returns:** An enumerable of input channel ID strings.

---

### GetAudioOutputIds()

```csharp
IEnumerable<string> GetAudioOutputIds()
```

Gets the IDs of all the output channels added to this device when created.

**Returns:** An enumerable of output channel ID strings.

---

### SetAudioInputLevel(string id, int level)

```csharp
void SetAudioInputLevel(string id, int level)
```

Set the target input channel to the given audio level. Range is 0–100 and scaled internally to match the device limits.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the input channel to adjust. |
| `level` | `int` | The 0–100 level to set the channel volume to. |

---

### GetAudioInputLevel(string id)

```csharp
int GetAudioInputLevel(string id)
```

Query the device for the current input audio level.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the input channel to query. |

**Returns:** A 0–100 value representing the current audio level. Returns `0` if `id` cannot be found.

---

### SetAudioInputMute(string id, bool mute)

```csharp
void SetAudioInputMute(string id, bool mute)
```

Send a mute command to the target input channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the channel to change. |
| `mute` | `bool` | `true` = mute on, `false` = mute off. |

---

### GetAudioInputMute(string id)

```csharp
bool GetAudioInputMute(string id)
```

Gets the current mute status of the target input channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the input channel to query. |

**Returns:** `true` if mute is active, `false` if mute is off.

---

### SetAudioOutputLevel(string id, int level)

```csharp
void SetAudioOutputLevel(string id, int level)
```

Set the target output channel to the given audio level. Range is 0–100 and scaled internally to match the device limits.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the output channel to adjust. |
| `level` | `int` | The 0–100 level to set the channel volume to. |

---

### GetAudioOutputLevel(string id)

```csharp
int GetAudioOutputLevel(string id)
```

Query the device for the current output audio level.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the output channel to query. |

**Returns:** A 0–100 value representing the current audio level. Returns `0` if `id` cannot be found.

---

### SetAudioOutputMute(string id, bool mute)

```csharp
void SetAudioOutputMute(string id, bool mute)
```

Send a mute command to the target output channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the channel to change. |
| `mute` | `bool` | `true` = mute on, `false` = mute off. |

---

### GetAudioOutputMute(string id)

```csharp
bool GetAudioOutputMute(string id)
```

Gets the current mute status of the target output channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the output channel to query. |

**Returns:** `true` if mute is active, `false` if mute is off.

---

### RecallAudioPreset(string id)

```csharp
void RecallAudioPreset(string id)
```

Attempts to recall the target preset on the device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the preset to recall, as defined in the system configuration. |

---

### AddInputChannel(...)

```csharp
void AddInputChannel(
    string id,
    string levelTag,
    string muteTag,
    int bankIndex,
    int levelMax,
    int levelMin,
    int routerIndex,
    List<string> tags)
```

Add an input or microphone channel to the DSP. The DSP implementation will then update its control methods for that channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the channel as defined in the configuration. |
| `levelTag` | `string` | The instance tag or named control associated with changing the gain. |
| `muteTag` | `string` | The instance tag or named control associated with the mute state. |
| `bankIndex` | `int` | The input number or position in a channel bank used for control. |
| `levelMax` | `int` | The maximum value expected by the hardware for the audio channel (device-native range). |
| `levelMin` | `int` | The minimum value expected by the hardware for the audio channel (device-native range). |
| `routerIndex` | `int` | The input index for this channel if routing is supported. Can be `0` if unused. |
| `tags` | `List<string>` | A collection of keywords used for plugin-specific or custom UI behavior. |

---

### AddOutputChannel(...)

```csharp
void AddOutputChannel(
    string id,
    string levelTag,
    string muteTag,
    string routerTag,
    int routerIndex,
    int bankIndex,
    int levelMax,
    int levelMin,
    List<string> tags)
```

Add an output to the DSP. The DSP implementation will then update its control methods for that channel.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the channel as defined in the configuration. |
| `levelTag` | `string` | The instance tag or named control associated with changing the gain. |
| `muteTag` | `string` | The instance tag or named control associated with the mute state. |
| `routerTag` | `string` | The instance tag or named control associated with a router block. |
| `routerIndex` | `int` | The output index of the router associated with this channel. Can be `0` if not routable. |
| `bankIndex` | `int` | The output number or position in a channel bank used for control. |
| `levelMax` | `int` | The maximum value expected by the hardware for the audio channel (device-native range). |
| `levelMin` | `int` | The minimum value expected by the hardware for the audio channel (device-native range). |
| `tags` | `List<string>` | A collection of keywords used for plugin-specific or custom UI behavior. |

---

### AddPreset(string id, string bank, int index)

```csharp
void AddPreset(string id, string bank, int index)
```

Add a preset recall to the DSP. The DSP implementation will update its control methods for that preset or snapshot.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the preset. This can either be a snapshot bank name or tag ID. |
| `bank` | `string` | The name or number of the snapshot/preset bank that the preset is associated with. |
| `index` | `int` | The index or preset number to recall. |
