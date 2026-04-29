# ITcpDevice

**Namespace:** `gcu_hardware_service.Communication`

Properties and methods common to all devices that are TCP-IP controlled.

---

## Table of Contents

**Methods**
- [SetTcpConnectionInfo(string host, int port)](#settcpconnectioninfostring-host-int-port)

---

## Methods

### SetTcpConnectionInfo(string host, int port)

```csharp
void SetTcpConnectionInfo(string host, int port)
```

Set the TCP connection information before calling `BaseDevice.Initialize()` and `BaseDevice.Connect()`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `host` | `string` | The IP address or hostname of the device. |
| `port` | `int` | The TCP port used to connect to the device. |
