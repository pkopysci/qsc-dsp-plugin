# IEndpointControlApp

**Namespace:** `gcu_application_service.EndpointControl`

Common properties and methods for endpoint control applications. Provides relay and connection management for I/O endpoint devices.

---

## Table of Contents

**Events**
- [EndpointRelayChanged](#endpointrelaychanged)
- [EndpointConnectionChanged](#endpointconnectionchanged)

**Methods**
- [PulseEndpointRelay(string id, int index, int timeMs)](#pulseendpointrelaystring-id-int-index-int-timems)
- [LatchRelayClosed(string id, int index)](#latchrelayclosedstring-id-int-index)
- [LatchRelayOpen(string id, int index)](#latchrelayopenstring-id-int-index)
- [GetAllEndpoints()](#getallendpoints)
- [GetEndpoint(string id)](#getendpointstring-id)

---

## Events

### EndpointRelayChanged

```csharp
event EventHandler<GenericDualEventArgs<string, int>> EndpointRelayChanged
```

Triggered whenever a relay on an endpoint changes state. `Arg1` is the endpoint device ID; `Arg2` is the relay index that changed.

---

### EndpointConnectionChanged

```csharp
event EventHandler<GenericDualEventArgs<string, bool>> EndpointConnectionChanged
```

Triggered whenever the control connection status changes for an endpoint. `Arg1` is the endpoint device ID; `Arg2` is the new connection state.

---

## Methods

### PulseEndpointRelay(string id, int index, int timeMs)

```csharp
void PulseEndpointRelay(string id, int index, int timeMs)
```

Close a relay on the target endpoint for the given amount of time, then reopen it.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the endpoint to control. |
| `index` | `int` | The 0-based relay index on the endpoint. |
| `timeMs` | `int` | The amount of time in milliseconds to latch the relay closed. |

---

### LatchRelayClosed(string id, int index)

```csharp
void LatchRelayClosed(string id, int index)
```

Set the relay closed on the target device until manually opened or pulsed.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the endpoint to control. |
| `index` | `int` | The 0-based relay index on the endpoint. |

---

### LatchRelayOpen(string id, int index)

```csharp
void LatchRelayOpen(string id, int index)
```

Set the relay open on the target device until manually closed or pulsed.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the endpoint to control. |
| `index` | `int` | The 0-based relay index on the endpoint. |

---

### GetAllEndpoints()

```csharp
List<InfoContainer> GetAllEndpoints()
```

Get information data on all endpoint devices in the system configuration.

**Returns:** All endpoint devices that were created at program boot.

---

### GetEndpoint(string id)

```csharp
InfoContainer GetEndpoint(string id)
```

Get information on a single endpoint device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the endpoint device to query. |

**Returns:** Information on the target endpoint if found; otherwise an empty `InfoContainer`.
