# ISerialEndpoint

**Namespace:** `gcu_hardware_service.EndpointDevices`

Required interface features for any endpoint device that supports serial IO (RS-232).

---

## Table of Contents

**Methods**
- [GetComPort(int port)](#getcomportint-port)

---

## Methods

### GetComPort(int port)

```csharp
ISerialPort? GetComPort(int port)
```

Get the target com port in the collection of available ports on the device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `port` | `int` | The index of the port to reference. Cannot be less than zero or greater than the total number of com ports supported by the device. |

**Returns:** The target com port, or `null` if the port could not be retrieved.

**Exceptions**

| Type | Condition |
|------|-----------|
| `ArgumentOutOfRangeException` | Thrown if `port` is outside the bounds of the com port collection. |
