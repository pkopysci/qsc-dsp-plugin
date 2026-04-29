# IAudioRoutable

**Namespace:** `gcu_hardware_service.Routable`

Common methods and attributes for all devices that can route audio.

---

## Table of Contents

**Events**
- [AudioRouteChanged](#audioroutechanged)

**Methods**
- [GetCurrentAudioSource(string outputId)](#getcurrentaudiosourcestring-outputid)
- [RouteAudio(string sourceId, string outputId)](#routeaudiostring-sourceid-string-outputid)
- [ClearAudioRoute(string outputId)](#clearaudioroutestring-outputid)

---

## Events

### AudioRouteChanged

```csharp
event EventHandler<GenericDualEventArgs<string, string>> AudioRouteChanged
```

Triggered when there is a change in the audio source for an output. Event args: arg1 = device ID, arg2 = output ID that changed.

---

## Methods

### GetCurrentAudioSource(string outputId)

```csharp
string GetCurrentAudioSource(string outputId)
```

Query the device for the audio input that is currently routed to the target output. An error will be written to the logging system if a failure occurs.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `outputId` | `string` | The output ID to query. |

**Returns:** The audio input ID that is currently routed, or an empty string if the query fails.

---

### RouteAudio(string sourceId, string outputId)

```csharp
void RouteAudio(string sourceId, string outputId)
```

Route the target audio input to the target audio output.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `sourceId` | `string` | The input ID that will be routed. |
| `outputId` | `string` | The output ID to route to. |

---

### ClearAudioRoute(string outputId)

```csharp
void ClearAudioRoute(string outputId)
```

Clear the output of all audio signals.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `outputId` | `string` | The output to clear audio content on. |
