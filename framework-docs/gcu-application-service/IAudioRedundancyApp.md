# IAudioRedundancyApp

**Namespace:** `gcu_application_service.AudioControl`

Required events and methods for supporting an audio device that includes backup/redundant device features.

---

## Table of Contents

**Events**
- [AudioDeviceRedundancyChanged](#audiodeviceredundancychanged)
- [AudioDeviceBackupConnectionChanged](#audiodevicebackupconnectionchanged)

**Methods**
- [GetRedundantAudioDevice(string id)](#getredundantaudiodevicestring-id)
- [GetAllRedundantAudioDevices()](#getallredundantaudiodevices)

---

## Events

### AudioDeviceRedundancyChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>>? AudioDeviceRedundancyChanged
```

Triggered when an audio device switches from the primary to the backup device, or from backup back to primary. The event arg is the ID of the audio device that changed.

---

### AudioDeviceBackupConnectionChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>>? AudioDeviceBackupConnectionChanged
```

Triggered when an audio device that includes a backup detects a connection change in the backup device. The event arg is the ID of the audio device that changed.

---

## Methods

### GetRedundantAudioDevice(string id)

```csharp
RedundantDeviceInfoContainer? GetRedundantAudioDevice(string id)
```

Attempt to get information on an audio device that supports redundancy.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the DSP or other audio device. |

**Returns:** A data object containing redundancy state information, or `null` if no match was found.

---

### GetAllRedundantAudioDevices()

```csharp
IReadOnlyCollection<RedundantDeviceInfoContainer> GetAllRedundantAudioDevices()
```

Get all audio devices that support redundant backup.

**Returns:** A collection of data objects representing all audio devices that support redundancy.
