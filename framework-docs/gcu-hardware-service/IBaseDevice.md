# IBaseDevice

**Namespace:** `gcu_hardware_service.BaseDevice`

Interface for attributes common to all devices. All hardware control interfaces in the framework extend this interface.

---

## Table of Contents

**Events**
- [ConnectionChanged](#connectionchanged)

**Properties**
- [Id](#id)
- [Label](#label)
- [IsOnline](#isonline)
- [IsInitialized](#isinitialized)
- [Manufacturer](#manufacturer)
- [Model](#model)

**Methods**
- [Connect()](#connect)
- [Disconnect()](#disconnect)

---

## Events

### ConnectionChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> ConnectionChanged
```

Notification for when the device connection has changed. The event argument contains the device ID.

---

## Properties

### Id

```csharp
string Id { get; }
```

Gets the unique ID of the device.

---

### Label

```csharp
string Label { get; }
```

Gets the user-friendly label of the device.

---

### IsOnline

```csharp
bool IsOnline { get; }
```

Gets a value indicating whether the device is online or not.

---

### IsInitialized

```csharp
bool IsInitialized { get; }
```

Gets a value indicating whether or not the device has been initialized and is ready to connect.

---

### Manufacturer

```csharp
string Manufacturer { get; set; }
```

The name of the company that created the device.

---

### Model

```csharp
string Model { get; set; }
```

The specific device/hardware name used by the manufacturer.

---

## Methods

### Connect()

```csharp
void Connect()
```

Connect the communications protocol to the hardware.

---

### Disconnect()

```csharp
void Disconnect()
```

Closes an active connection/communications protocol with the hardware.
