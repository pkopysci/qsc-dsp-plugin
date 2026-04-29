# IVideoFreezeDevice

**Namespace:** `gcu_hardware_service.DisplayDevices`

Common properties and methods for a device that supports video freeze features.

---

## Table of Contents

**Events**
- [VideoFreezeChanged](#videofreezechanged)

**Properties**
- [FreezeState](#freezestate)

**Methods**
- [FreezeOn()](#freezeon)
- [FreezeOff()](#freezeoff)

---

## Events

### VideoFreezeChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> VideoFreezeChanged
```

Event triggered whenever a video freeze status is reported by the display (if supported). The event argument contains the device ID.

---

## Properties

### FreezeState

```csharp
bool FreezeState { get; }
```

Gets a value indicating whether the video freeze is on or off. `true` = video frozen; `false` = in motion. Will be `false` if freeze is not supported.

---

## Methods

### FreezeOn()

```csharp
void FreezeOn()
```

If supported, sends the "freeze video on" command to the display hardware. Does nothing if video freeze is not supported.

---

### FreezeOff()

```csharp
void FreezeOff()
```

If supported, sends the "freeze video off" command to the display hardware. Does nothing if video freeze is not supported.
