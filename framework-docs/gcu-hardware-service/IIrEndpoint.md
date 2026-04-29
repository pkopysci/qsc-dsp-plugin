# IIrEndpoint

**Namespace:** `gcu_hardware_service.EndpointDevices`

Required interface features for any endpoint device that supports IR output.

---

## Table of Contents

**Methods**
- [GetIrPort(int port)](#getirportint-port)

---

## Methods

### GetIrPort(int port)

```csharp
IIrPort GetIrPort(int port)
```

Get the target IR port in the collection of available ports on the device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `port` | `int` | The index of the port to reference. |

**Returns:** The target IR port if it exists.

**Exceptions**

| Type | Condition |
|------|-----------|
| `ArgumentOutOfRangeException` | Thrown if `port` is outside the bounds of the IR port collection. |
