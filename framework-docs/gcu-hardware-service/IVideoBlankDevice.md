# IVideoBlankDevice

**Namespace:** `gcu_hardware_service.DisplayDevices`

Required events, properties, and methods for a device that supports video blank features.

---

## Table of Contents

**Events**
- [VideoBlankChanged](#videoblankchanged)

**Properties**
- [BlankState](#blankstate)

**Methods**
- [VideoBlankOn()](#videoblankon)
- [VideoBlankOff()](#videoblankoff)

---

## Events

### VideoBlankChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>>? VideoBlankChanged
```

Event triggered whenever a video blank status is reported by the display. The event argument contains the device ID.

---

## Properties

### BlankState

```csharp
bool BlankState { get; }
```

Gets a value indicating whether the video blank is on or off. `true` = video is blanked; `false` = video is active.

---

## Methods

### VideoBlankOn()

```csharp
void VideoBlankOn()
```

Enable video blank on the display hardware (no picture shown).

---

### VideoBlankOff()

```csharp
void VideoBlankOff()
```

Disable video blank on the display hardware (picture is visible).
