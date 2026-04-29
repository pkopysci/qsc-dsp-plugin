# IAvIpEndpoint

**Namespace:** `gcu_hardware_service.AvIpMatrix`

Required methods and properties for implementing a transmitter or receiver AV-IP endpoint.

---

## Table of Contents

**Properties**
- [Id](#id)
- [Label](#label)
- [Manufacturer](#manufacturer)
- [Model](#model)
- [IsOnline](#isonline)
- [EndpointType](#endpointtype)

---

## Properties

### Id

```csharp
string Id { get; }
```

The unique ID of this device.

---

### Label

```csharp
string Label { get; }
```

The user-friendly name of the device.

---

### Manufacturer

```csharp
string Manufacturer { get; }
```

The company that makes the device.

---

### Model

```csharp
string Model { get; }
```

The model name of the device.

---

### IsOnline

```csharp
bool IsOnline { get; }
```

Gets a value indicating whether the AV-over-IP endpoint is online (`true`) or offline (`false`).

---

### EndpointType

```csharp
AvIpEndpointTypes EndpointType { get; }
```

Gets what type of endpoint the device is (encoder or decoder). See [`AvIpEndpointTypes`](AvIpEndpointTypes.md).
