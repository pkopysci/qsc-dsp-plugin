# DeviceContainer\<T\>

**Namespace:** `gcu_hardware_service.BaseDevice`

**Implements:** `IDisposable`

Manager class for adding, removing, and finding a device of the given type.

**Type Parameters**

| Name | Description |
|------|-------------|
| `T` | The device type that will be managed by this container. |

---

## Table of Contents

**Methods**
- [GetDevice(string id)](#getdevicestring-id)
- [GetAllDevices()](#getalldevices)
- [ContainsDevice(string id)](#containsdevicestring-id)
- [AddDevice(string id, T device)](#adddevicestring-id-T-device)
- [Dispose()](#dispose)

---

## Methods

### GetDevice(string id)

```csharp
public T? GetDevice(string id)
```

Attempt to retrieve the device with the given ID. Writes an error to the logging system if no device is found at the given ID.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the device to get. |

**Returns:** The device object if found, otherwise the default value of `T`.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `id` is null or empty. |

---

### GetAllDevices()

```csharp
public ReadOnlyCollection<T> GetAllDevices()
```

Gets a collection of all devices currently stored in this container.

**Returns:** A read-only collection of all currently stored devices.

---

### ContainsDevice(string id)

```csharp
public bool ContainsDevice(string id)
```

Checks to see if a device exists with the given ID.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the device to check. |

**Returns:** `true` if the device is found, `false` otherwise.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `id` is null or empty. |

---

### AddDevice(string id, T device)

```csharp
public void AddDevice(string id, T device)
```

Adds a new device to the container. If a device with the matching ID already exists then it will be replaced.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID used to reference the device. |
| `device` | `T` | The device that will be added to the container. |

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `id` is null or empty. |
| `ArgumentNullException` | If `device` is null. |

---

### Dispose()

```csharp
public void Dispose()
```

Disposes all devices stored in the container that implement `IDisposable`.
