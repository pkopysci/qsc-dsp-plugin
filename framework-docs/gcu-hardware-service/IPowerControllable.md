# IPowerControllable

**Namespace:** `gcu_hardware_service.PowerControl`

Events, properties, and methods for devices that allow for power on/off control.

---

## Table of Contents

**Events**
- [PowerChanged](#powerchanged)

**Properties**
- [PowerState](#powerstate)

**Methods**
- [PowerOn()](#poweron)
- [PowerOff()](#poweroff)

---

## Events

### PowerChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> PowerChanged
```

Triggered when the power state for the device changes. The event argument contains the ID of the device that changed.

---

## Properties

### PowerState

```csharp
bool PowerState { get; }
```

`true` = device is powered on; `false` = device is powered off or in standby.

---

## Methods

### PowerOn()

```csharp
void PowerOn()
```

Send a command to turn the device on.

---

### PowerOff()

```csharp
void PowerOff()
```

Send a command to turn the device off/standby.
