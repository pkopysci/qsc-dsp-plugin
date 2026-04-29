# IWakeOnLanDevice

**Namespace:** `gcu_hardware_service.Communication`

Required properties and methods for any device that requires Wake-On-LAN for power on.

---

## Table of Contents

**Methods**
- [SetWakeOnLanData(byte\[\] macAddress, string broadcastAddress, uint broadcastPort)](#setwakeonlandatabyte-macaddress-string-broadcastaddress-uint-broadcastport)

---

## Methods

### SetWakeOnLanData(byte\[\] macAddress, string broadcastAddress, uint broadcastPort)

```csharp
void SetWakeOnLanData(byte[] macAddress, string broadcastAddress = "255.255.255.255", uint broadcastPort = 9)
```

Set data that will be used when sending the magic packet to the device. Must be called before `Initialize()`.

**Parameters**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `macAddress` | `byte[]` | | The MAC address of the device in byte array form. |
| `broadcastAddress` | `string` | `"255.255.255.255"` | The target broadcast address. |
| `broadcastPort` | `uint` | `9` | The port that the device is listening on. |
