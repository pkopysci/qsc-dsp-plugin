# IVideoRoutable

**Namespace:** `gcu_hardware_service.Routable`

Common methods and attributes for all devices that can route video.

---

## Table of Contents

**Events**
- [VideoRouteChanged](#videoroutechanged)

**Methods**
- [GetCurrentVideoSource(uint output)](#getcurrentvideosourceuint-output)
- [RouteVideo(uint source, uint output)](#routevideouint-source-uint-output)
- [ClearVideoRoute(uint output)](#clearvideorouteuint-output)

---

## Events

### VideoRouteChanged

```csharp
event EventHandler<GenericDualEventArgs<string, uint>> VideoRouteChanged
```

Triggered when there is a change in the video source for an output. Event args: arg1 = device ID, arg2 = output number that changed.

---

## Methods

### GetCurrentVideoSource(uint output)

```csharp
uint GetCurrentVideoSource(uint output)
```

Query the device for the video input that is currently routed to the target output. An error will be written to the logging system if a failure occurs.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `output` | `uint` | The output number to query. |

**Returns:** The video input number that is currently routed, or `0` if the query fails.

---

### RouteVideo(uint source, uint output)

```csharp
void RouteVideo(uint source, uint output)
```

Route the target video input to the target video output.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `source` | `uint` | The input number that will be routed. |
| `output` | `uint` | The output number to route to. |

---

### ClearVideoRoute(uint output)

```csharp
void ClearVideoRoute(uint output)
```

Clear the output of all video signals.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `output` | `uint` | The output to clear video content on. |
