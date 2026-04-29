# ISupportsHoursUsed

**Namespace:** `gcu_hardware_service.DisplayDevices`

Required events and properties for a device that supports usage tracking (e.g., projector lamp hours).

---

## Table of Contents

**Events**
- [HoursUsedChanged](#hoursusedchanged)

**Properties**
- [HoursUsed](#hoursused)

---

## Events

### HoursUsedChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>>? HoursUsedChanged
```

Event triggered when a "lamp hours" update is reported by the display driver. The event argument contains the device ID.

---

## Properties

### HoursUsed

```csharp
uint HoursUsed { get; }
```

Gets the number of hours used as of the last lamp hours response from the driver.
