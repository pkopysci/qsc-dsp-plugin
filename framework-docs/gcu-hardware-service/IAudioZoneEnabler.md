# IAudioZoneEnabler

**Namespace:** `gcu_hardware_service.AudioDevices`

Events, properties, and methods for controlling which audio output zones a given input channel should be sending signal to. This is an optional interface that DSP plugins may implement alongside [`IDsp`](IDsp.md).

---

## Table of Contents

**Events**
- [AudioZoneEnableChanged](#audiozoneenablechanged)

**Methods**
- [AddAudioZoneEnable(string channelId, string zoneId, string controlTag)](#addaudiozoneenablestring-channelid-string-zoneid-string-controltag)
- [RemoveAudioZoneEnable(string channelId, string zoneId)](#removeaudiozoneenablestring-channelid-string-zoneid)
- [ToggleAudioZoneEnable(string channelId, string zoneId)](#toggleaudiozoneenablestring-channelid-string-zoneid)
- [SetAudioZoneEnable(string channelId, string zoneId, bool enable)](#setaudiozoneenablestring-channelid-string-zoneid-bool-enable)
- [QueryAudioZoneEnable(string channelId, string zoneId)](#queryaudiozoneenablestring-channelid-string-zoneid)

---

## Events

### AudioZoneEnableChanged

```csharp
event EventHandler<GenericDualEventArgs<string, string>> AudioZoneEnableChanged
```

Triggered when the device control detects a change on a channel audio zone enable. Event args: arg1 = channel ID, arg2 = zone toggle ID.

---

## Methods

### AddAudioZoneEnable(string channelId, string zoneId, string controlTag)

```csharp
void AddAudioZoneEnable(string channelId, string zoneId, string controlTag)
```

Add an audio zone toggle control to the internal collection of the control object. If a control object with matching `channelId` and `zoneId` is detected then the new one will be ignored.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `channelId` | `string` | The unique ID of the input channel this control will be associated with. |
| `zoneId` | `string` | The unique ID of the toggle control object. This is used internally for referencing. |
| `controlTag` | `string` | The DSP design named control or Instance ID used for device control. |

---

### RemoveAudioZoneEnable(string channelId, string zoneId)

```csharp
void RemoveAudioZoneEnable(string channelId, string zoneId)
```

Remove an audio zone toggle control from the internal collection. If no object is found with a matching `channelId` and `zoneId` then no action is taken.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `channelId` | `string` | The unique ID of the input channel associated with the control being removed. |
| `zoneId` | `string` | The unique ID of the zone enable control being removed. |

---

### ToggleAudioZoneEnable(string channelId, string zoneId)

```csharp
void ToggleAudioZoneEnable(string channelId, string zoneId)
```

Send a command to the hardware to toggle the current state of the zone enable control. If no matching `channelId` and `zoneId` is found then no action is taken.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `channelId` | `string` | The unique ID associated with the zone enable control. |
| `zoneId` | `string` | The unique ID of the zone to send the change request to. |

---

### SetAudioZoneEnable(string channelId, string zoneId, bool enable)

```csharp
void SetAudioZoneEnable(string channelId, string zoneId, bool enable)
```

Discretely set whether an audio channel is mixed to a given output zone.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `channelId` | `string` | The unique ID associated with the zone enable control. |
| `zoneId` | `string` | The unique ID of the zone to send the change request to. |
| `enable` | `bool` | `true` = enable the channel mix, `false` = mute/disable the channel mix. |

---

### QueryAudioZoneEnable(string channelId, string zoneId)

```csharp
bool QueryAudioZoneEnable(string channelId, string zoneId)
```

Queries the device for the current status of the target zone enable control.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `channelId` | `string` | The unique ID associated with the zone enable control. |
| `zoneId` | `string` | The unique ID of the zone control object being queried. |

**Returns:** The current state of the zone enable control. Returns `false` if a `channelId`/`zoneId` is not found.
