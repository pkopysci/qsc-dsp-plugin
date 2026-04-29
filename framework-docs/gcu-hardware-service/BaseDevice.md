# BaseDevice

**Namespace:** `gcu_hardware_service.BaseDevice`

**Implements:** [`IBaseDevice`](IBaseDevice.md)

Abstract base class for representing hardware controls. All concrete device classes in the framework extend this class.

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
- [NotifyOnlineStatus()](#notifyonlinestatus)

---

## Events

### ConnectionChanged

```csharp
public event EventHandler<GenericSingleEventArgs<string>>? ConnectionChanged
```

Notification for when the device connection has changed. The event argument contains the device ID.

---

## Properties

### Id

```csharp
public string Id { get; protected set; }
```

The unique ID of the device. Defaults to `string.Empty`.

---

### Label

```csharp
public string Label { get; protected set; }
```

The user-friendly label of the device. Defaults to `string.Empty`.

---

### IsOnline

```csharp
public virtual bool IsOnline { get; protected set; }
```

Gets a value indicating whether the device is online or not.

---

### IsInitialized

```csharp
public virtual bool IsInitialized { get; protected set; }
```

Gets a value indicating whether the device has been initialized and is ready to connect.

---

### Manufacturer

```csharp
public string Manufacturer { get; set; }
```

The name of the company that created the device. Defaults to `string.Empty`.

---

### Model

```csharp
public string Model { get; set; }
```

The specific device/hardware name used by the manufacturer. Defaults to `string.Empty`.

---

## Methods

### Connect()

```csharp
public virtual void Connect()
```

Connect the communications protocol to the hardware. The base implementation is a no-op; override in subclasses.

---

### Disconnect()

```csharp
public virtual void Disconnect()
```

Closes an active connection/communications protocol with the hardware. The base implementation is a no-op; override in subclasses.

---

### NotifyOnlineStatus()

```csharp
protected virtual void NotifyOnlineStatus()
```

Method for notifying subscribers that the device online status has changed.
