# BaseApp\<TDevice, TDeviceData\>

**Namespace:** `gcu_application_service.Base`

**Implements:** `IDisposable`

Generic base class that pairs a hardware device collection with its configuration data. Implements common device lookup helpers used by all application control sub-components.

**Type Parameters**

| Name | Constraint | Description |
|------|-----------|-------------|
| `TDevice` | `IBaseDevice` | The hardware device interface type stored in the container. |
| `TDeviceData` | `BaseData` | The domain data type representing configuration for the devices. |

---

## Table of Contents

**Constructors**
- [BaseApp(DeviceContainer\<TDevice\>, ReadOnlyCollection\<TDeviceData\>)](#baseappdevicecontainertdevice-readonlycollectiontdevicedata)

**Protected Properties**
- [Devices](#devices)
- [Data](#data)

**Protected Methods**
- [GetDevice(string id)](#getdevicestring-id)
- [GetDeviceInfo(string id)](#getdeviceinfostring-id)
- [GetAllDevices()](#getalldevices)
- [Dispose(bool disposing)](#disposebool-disposing)

**Public Methods**
- [Dispose()](#dispose)

---

## Constructors

### BaseApp(DeviceContainer\<TDevice\>, ReadOnlyCollection\<TDeviceData\>)

```csharp
public BaseApp(DeviceContainer<TDevice> devices, ReadOnlyCollection<TDeviceData> data)
```

Initializes a new instance with the hardware device collection and domain configuration data.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `devices` | `DeviceContainer<TDevice>` | The collection of devices that will be controlled by this app. |
| `data` | `ReadOnlyCollection<TDeviceData>` | The collection of config data associated with the devices. |

---

## Protected Properties

### Devices

```csharp
protected readonly DeviceContainer<TDevice> Devices
```

Container for managing hardware interaction objects.

---

### Data

```csharp
protected readonly ReadOnlyCollection<TDeviceData> Data
```

Collection of configuration data representing the devices.

---

## Protected Methods

### GetDevice(string id)

```csharp
protected TDevice? GetDevice(string id)
```

Attempt to find the specific device in the collection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the device to find. |

**Returns:** The device control object if found, otherwise `null`.

---

### GetDeviceInfo(string id)

```csharp
protected TDeviceData? GetDeviceInfo(string id)
```

Get the configuration data for the target device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the device to look for. |

**Returns:** The configuration data for the device, or `null` if not found.

---

### GetAllDevices()

```csharp
protected ReadOnlyCollection<TDevice> GetAllDevices()
```

Get all devices in the collection.

**Returns:** A read-only collection of all devices.

---

### Dispose(bool disposing)

```csharp
protected void Dispose(bool disposing)
```

Dispose all managed objects.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `disposing` | `bool` | `true` = this object is actively disposing; `false` = called from finalizer. |

---

## Public Methods

### Dispose()

```csharp
public void Dispose()
```

Releases all resources used by this instance.
