# IEndpointDevice

**Namespace:** `gcu_hardware_service.EndpointDevices`

**Implements:** [`IBaseDevice`](IBaseDevice.md)

Common properties and methods for all endpoint devices (such as DM-TX and RMC-100s).

---

## Table of Contents

**Properties**
- [IsRegistered](#isregistered)
- [SupportsRelays](#supportsrelays)
- [SupportsRs232](#supportsrs232)

**Methods**
- [Initialize(Endpoint configData)](#initializeendpoint-configdata)
- [Register()](#register)

---

## Properties

### IsRegistered

```csharp
bool IsRegistered { get; }
```

`true` if the device has been registered with the control system; otherwise `false`.

---

### SupportsRelays

```csharp
bool SupportsRelays { get; }
```

`true` if the endpoint supports relay controls; otherwise `false`.

---

### SupportsRs232

```csharp
bool SupportsRs232 { get; }
```

`true` if the endpoint supports RS-232 controls; otherwise `false`.

---

## Methods

### Initialize(Endpoint configData)

```csharp
void Initialize(Endpoint configData)
```

Initialize the internal control objects based on the given configuration information.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `configData` | `Endpoint` | Data object containing connection and port information. |

---

### Register()

```csharp
void Register()
```

Register any connections or control interfaces on the underlying hardware control.
