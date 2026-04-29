# IAvIpMatrix

**Namespace:** `gcu_hardware_service.AvIpMatrix`

Required events, methods, and properties for implementing an AV-over-IP device control.

---

## Table of Contents

**Events**
- [AvIpEndpointStatusChanged](#avipendpointstatuschanged)

**Methods**
- [GetAllAvIpEndpoints()](#getallavipendpoints)
- [GetAvIpEndpoint(string deviceId)](#getavipendpointstring-deviceid)
- [AddEndpoint(...)](#addendpoint)

---

## Events

### AvIpEndpointStatusChanged

```csharp
event EventHandler<GenericDualEventArgs<string, string>> AvIpEndpointStatusChanged
```

Triggered when an encoder or decoder status changes, typically used for online/offline events. Event args: arg1 = device ID, arg2 = endpoint ID.

---

## Methods

### GetAllAvIpEndpoints()

```csharp
ReadOnlyCollection<IAvIpEndpoint> GetAllAvIpEndpoints()
```

Returns all AV/IP endpoints that have been assigned to this device.

**Returns:** A read-only collection of all assigned [`IAvIpEndpoint`](IAvIpEndpoint.md) objects.

---

### GetAvIpEndpoint(string deviceId)

```csharp
IAvIpEndpoint? GetAvIpEndpoint(string deviceId)
```

Get the target AV-over-IP endpoint data object.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `deviceId` | `string` | The unique ID of the endpoint to request. |

**Returns:** The target [`IAvIpEndpoint`](IAvIpEndpoint.md), or `null` if the endpoint cannot be found.

---

### AddEndpoint(...)

```csharp
void AddEndpoint(
    string id,
    List<string> tags,
    int ioIndex,
    AvIpEndpointTypes endpointType,
    CrestronControlSystem control)
```

Add an AV-over-IP endpoint to the routing object.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the endpoint. Used internally for routing events. |
| `tags` | `List<string>` | The collection of device tags set in the configuration file. |
| `ioIndex` | `int` | The input or output index on the hardware associated with this routing endpoint. |
| `endpointType` | [`AvIpEndpointTypes`](AvIpEndpointTypes.md) | Whether this will be added as an encoder or decoder endpoint. |
| `control` | `CrestronControlSystem` | The root control system object that runs this program. |
