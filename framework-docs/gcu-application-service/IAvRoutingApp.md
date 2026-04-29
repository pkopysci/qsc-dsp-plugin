# IAvRoutingApp

**Namespace:** `gcu_application_service.AvRouting`

Common properties and methods for audio and video routing applications.

---

## Table of Contents

**Events**
- [RouteChanged](#routechanged)
- [RouterConnectChange](#routerconnectchange)
- [VideoInputSyncChanged](#videoinputsyncchanged)

**Methods**
- [QueryRouterConnectionStatus(string id)](#queryrouterconnectionstatusstring-id)
- [QueryVideoInputSyncStatus(string id)](#queryvideoinputsyncstatusstring-id)
- [GetAllAvSources()](#getallavsources)
- [GetAllAvDestinations()](#getallavdestinations)
- [GetAllAvRouters()](#getallavrouters)
- [MakeRoute(string inputId, string outputId)](#makeroutestring-inputid-string-outputid)
- [RouteToAll(string inputId)](#routetoallstring-inputid)
- [ReportGraph()](#reportgraph)
- [QueryCurrentRoute(string outputId)](#querycurrentroutestring-outputid)

---

## Events

### RouteChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> RouteChanged
```

Triggered when a destination in the routing map changes what input is displayed. The event arg is the ID of the destination that changed.

---

### RouterConnectChange

```csharp
event EventHandler<GenericSingleEventArgs<string>> RouterConnectChange
```

Triggered when an AV routing device comes online or goes offline. The event arg is the ID of the device that changed.

---

### VideoInputSyncChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>>? VideoInputSyncChanged
```

Triggered when a video input sync drops or is established on an AVR device that supports `IVideoInputSyncDevice`. The event arg is the ID of the video input that changed.

---

## Methods

### QueryRouterConnectionStatus(string id)

```csharp
bool QueryRouterConnectionStatus(string id)
```

Request the current online/offline status of the target AV routing device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the device to query. |

**Returns:** `true` if the device is online; `false` otherwise.

---

### QueryVideoInputSyncStatus(string id)

```csharp
bool QueryVideoInputSyncStatus(string id)
```

Query the sync status of a video input.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the video source to query. |

**Returns:** `true` if sync is detected; `false` if not, or if the input does not support sync detection.

---

### GetAllAvSources()

```csharp
ReadOnlyCollection<AvSourceInfoContainer> GetAllAvSources()
```

Query the service for all routable audio/video inputs.

**Returns:** A data collection of all AV inputs in the system configuration.

---

### GetAllAvDestinations()

```csharp
ReadOnlyCollection<InfoContainer> GetAllAvDestinations()
```

Query the service for all routable audio/video outputs.

**Returns:** A data collection of all AV outputs in the system configuration.

---

### GetAllAvRouters()

```csharp
ReadOnlyCollection<InfoContainer> GetAllAvRouters()
```

Query the service for all AVR devices.

**Returns:** A data collection representing all AV routing devices in the configuration.

---

### MakeRoute(string inputId, string outputId)

```csharp
void MakeRoute(string inputId, string outputId)
```

Request to route the target input to the target output.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `inputId` | `string` | The unique ID of the input to route. |
| `outputId` | `string` | The unique ID of the output to route to. |

---

### RouteToAll(string inputId)

```csharp
void RouteToAll(string inputId)
```

Route the target input to all destinations in the system.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `inputId` | `string` | The unique ID of the input to route. |

---

### ReportGraph()

```csharp
void ReportGraph()
```

Print out the current state of the routing graph to the log.

---

### QueryCurrentRoute(string outputId)

```csharp
AvSourceInfoContainer QueryCurrentRoute(string outputId)
```

Query the routing system for what input is currently routed to the given output. If no route is made the returned object will have an ID of `"NONE"`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `outputId` | `string` | The target output to query. |

**Returns:** An information container with data about the currently routed source.
