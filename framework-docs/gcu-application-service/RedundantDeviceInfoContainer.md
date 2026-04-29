# RedundantDeviceInfoContainer

**Namespace:** `gcu_application_service.Base`

**Inherits:** [`InfoContainer`](InfoContainer.md)

Data transfer object for any device that supports redundancy/backup switching.

---

## Table of Contents

**Constructors**
- [RedundantDeviceInfoContainer(...)](#redundantdeviceinfocontainer-1)

**Properties**
- [RedundantDeviceActive](#redundantdeviceactive)
- [RedundantDeviceOnline](#redundantdeviceonline)
- [RedundantDeviceExists](#redundantdeviceexists)

---

## Constructors

### RedundantDeviceInfoContainer(...)

```csharp
public RedundantDeviceInfoContainer(
    string id,
    string label,
    string icon,
    List<string> tags,
    bool isOnline)
```

Instantiates a new instance of `RedundantDeviceInfoContainer`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the device. Used for internal referencing. |
| `label` | `string` | The user-friendly name of the device. |
| `icon` | `string` | The image tag used for referencing the UI icon. |
| `tags` | `List<string>` | A collection of custom tags used by the subscribed service. |
| `isOnline` | `bool` | `true` = the device is currently connected; `false` = device offline. |

---

## Properties

### RedundantDeviceActive

```csharp
public bool RedundantDeviceActive { get; init; }
```

`true` if the backup device is currently active, indicating a possible issue with the primary device.

---

### RedundantDeviceOnline

```csharp
public bool RedundantDeviceOnline { get; init; }
```

`true` if the backup device is online; `false` if a connection to the backup device cannot be established.

---

### RedundantDeviceExists

```csharp
public bool RedundantDeviceExists { get; init; }
```

`true` if a backup device has been configured and should be monitored.
