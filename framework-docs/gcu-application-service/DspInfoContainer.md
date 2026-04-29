# DspInfoContainer

**Namespace:** `gcu_application_service.AudioControl`

**Inherits:** [`InfoContainer`](InfoContainer.md)

Data transfer object for audio DSP devices.

---

## Table of Contents

**Constructors**
- [DspInfoContainer(...)](#dspinfocontainer-1)

**Properties**
- [Presets](#presets)

---

## Constructors

### DspInfoContainer(...)

```csharp
public DspInfoContainer(string id, string label, string icon, List<string> tags, bool isOnline = false)
```

Creates a new instance of `DspInfoContainer`.

**Parameters**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `id` | `string` | | The unique ID of the DSP. Used for internal referencing. |
| `label` | `string` | | The user-friendly name of the DSP device. |
| `icon` | `string` | | The image tag used for referencing the UI icon. |
| `tags` | `List<string>` | | A collection of custom tags used by the subscribed service. |
| `isOnline` | `bool` | `false` | `true` = device is currently connected; `false` = device offline. |

---

## Properties

### Presets

```csharp
public List<InfoContainer> Presets { get; init; }
```

A collection of all triggerable presets associated with the device.
