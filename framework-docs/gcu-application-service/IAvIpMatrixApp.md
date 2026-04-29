# IAvIpMatrixApp

**Namespace:** `gcu_application_service.AvRouting`

Required interface for any application service that exposes details for an AVR that implements `IAvIpMatrix`.

---

## Table of Contents

**Events**
- [AvIpEndpointConnectionChanged](#avipendpointconnectionchanged)

**Properties**
- [AvIpRouterExists](#aviprouterexists)

**Methods**
- [GetAllAvIpEndpoints()](#getallavipendpoints)
- [TryGetAvIpEndpointInfoContainer(string id, out AvIpEndpointInfoContainer deviceInfo)](#trygetavipendpointinfocontainerstring-id-out-avipendpointinfocontainer-deviceinfo)

---

## Events

### AvIpEndpointConnectionChanged

```csharp
event EventHandler<GenericDualEventArgs<string, string>>? AvIpEndpointConnectionChanged
```

Triggered when an endpoint online/offline status change is reported by an AV-over-IP AVR device. `Arg1` is the AVR ID; `Arg2` is the endpoint ID.

---

## Properties

### AvIpRouterExists

```csharp
bool AvIpRouterExists { get; }
```

`true` if there is an AV-over-IP router in the configuration; otherwise `false`.

---

## Methods

### GetAllAvIpEndpoints()

```csharp
ReadOnlyCollection<AvIpEndpointInfoContainer> GetAllAvIpEndpoints()
```

Get all AV-over-IP endpoints in the system configuration.

**Returns:** A collection of data objects representing all AV/IP endpoints in the system.

---

### TryGetAvIpEndpointInfoContainer(string id, out AvIpEndpointInfoContainer deviceInfo)

```csharp
bool TryGetAvIpEndpointInfoContainer(string id, out AvIpEndpointInfoContainer deviceInfo)
```

Attempt to retrieve the info container for a specific AV-over-IP endpoint by ID.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the endpoint to retrieve. |
| `deviceInfo` | `out AvIpEndpointInfoContainer` | The reference to store the device data if found. |

**Returns:** `true` if AV-IP endpoints exist and a match was found; `false` otherwise.
