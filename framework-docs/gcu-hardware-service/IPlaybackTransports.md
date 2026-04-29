# IPlaybackTransports

**Namespace:** `gcu_hardware_service.TransportDevices`

Playback transport controls for devices that support media playback (Blu-ray, DVD, etc.).

---

## Table of Contents

**Properties**
- [SupportsEject](#supportseject)
- [SupportsRecord](#supportsrecord)

**Methods**
- [Play()](#play)
- [Pause()](#pause)
- [Stop()](#stop)
- [Record()](#record)
- [ScanForward()](#scanforward)
- [ScanReverse()](#scanreverse)
- [SkipForward()](#skipforward)
- [SkipReverse()](#skipreverse)
- [Eject()](#eject)

---

## Properties

### SupportsEject

```csharp
bool SupportsEject { get; }
```

`true` if the device supports an eject command; otherwise `false`.

---

### SupportsRecord

```csharp
bool SupportsRecord { get; }
```

`true` if the device supports a record command; otherwise `false`.

---

## Methods

### Play()

```csharp
void Play()
```

Send a play command to the device.

---

### Pause()

```csharp
void Pause()
```

Send a pause command to the device.

---

### Stop()

```csharp
void Stop()
```

Send a stop command to the device.

---

### Record()

```csharp
void Record()
```

Send a record command to the device. Only applicable when `SupportsRecord` is `true`.

---

### ScanForward()

```csharp
void ScanForward()
```

Send a fast-forward/scan forward command to the device.

---

### ScanReverse()

```csharp
void ScanReverse()
```

Send a rewind/scan reverse command to the device.

---

### SkipForward()

```csharp
void SkipForward()
```

Send a skip forward (next chapter/track) command to the device.

---

### SkipReverse()

```csharp
void SkipReverse()
```

Send a skip reverse (previous chapter/track) command to the device.

---

### Eject()

```csharp
void Eject()
```

Send an eject command to the device. Only applicable when `SupportsEject` is `true`.
