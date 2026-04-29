# AvSwitchCommands

**Namespace:** `gcu_hardware_service.AvSwitchDevices`

Command types used by various AV switchers for device control.

---

## Table of Contents

- [Enum Values](#enum-values)

---

## Enum Values

```csharp
public enum AvSwitchCommands
```

| Value | Description |
|-------|-------------|
| `RouteVideo` | Route a video input to an output. |
| `RouteAudio` | Route an audio input to an output. |
| `VideoRouteQuery` | Query the current video routing state. |
| `AudioRouteQuery` | Query the current audio routing state. |
| `VideoBlankOn` | Enable video blank on an output. |
| `VideoBlankOff` | Disable video blank on an output. |
| `VideoBlankToggle` | Toggle the video blank state. |
| `VideoBlankQuery` | Query the current video blank state. |
| `VideoFreezeOn` | Enable video freeze on an output. |
| `VideoFreezeOff` | Disable video freeze on an output. |
| `VideoFreezeToggle` | Toggle the video freeze state. |
| `VideoFreezeQuery` | Query the current video freeze state. |
| `AudioMuteOn` | Mute an audio output. |
| `AudioMuteOff` | Unmute an audio output. |
| `AudioMuteToggle` | Toggle the audio mute state. |
| `AudioMuteQuery` | Query the current audio mute state. |
| `VolumeSet` | Set the audio volume level. |
| `VolumeQuery` | Query the current audio volume level. |
| `Handshake` | Send a handshake or keepalive command. |
