# WakeOnLan

**Namespace:** `gcu_common_utils.NetComs`

**Implements:** `IDisposable`

Helper object for sending standard Wake-On-LAN (WOL) magic packets to a device. Supports broadcasting over either the LAN adapter or the Control subnet adapter on a Crestron control system.

See also: [Wake-on-LAN (Wikipedia)](https://en.wikipedia.org/wiki/Wake-on-LAN)

---

## Table of Contents

**Methods**
- [SendWakeOnLan(byte[] macAddress, string broadcastAddress)](#sendwakeonlanbyte-macaddress-string-broadcastaddress)
- [SendWakeOnSubnet(byte[] macAddress, string broadcastAddress)](#sendwakeonsubnetbyte-macaddress-string-broadcastaddress)
- [Dispose()](#dispose)

---

## Methods

### SendWakeOnLan(byte[] macAddress, string broadcastAddress)

```csharp
public void SendWakeOnLan(byte[] macAddress, string broadcastAddress = "255.255.255.255")
```

Send a Wake-On-LAN magic packet through the ethernet LAN adapter on the control system.

**Parameters**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `macAddress` | `byte[]` | — | The MAC address of the target device as a 6-byte array. |
| `broadcastAddress` | `string` | `"255.255.255.255"` | The UDP broadcast address to send the packet to. |

---

### SendWakeOnSubnet(byte[] macAddress, string broadcastAddress)

```csharp
public void SendWakeOnSubnet(byte[] macAddress, string broadcastAddress = "255.255.255.255")
```

Send a Wake-On-LAN magic packet through the Control subnet adapter on the control system.

**Parameters**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `macAddress` | `byte[]` | — | The MAC address of the target device as a 6-byte array. |
| `broadcastAddress` | `string` | `"255.255.255.255"` | The UDP broadcast address to send the packet to. |

**Remarks:** This method does not check for control subnet support and may cause an exception if the control system does not have a control subnet adapter. The calling class should verify compatibility before calling `SendWakeOnSubnet()`.

---

### Dispose()

```csharp
public void Dispose()
```

Releases all resources used by the `WakeOnLan` instance, including disabling and disposing the internal UDP server.
