# Audio

**Namespace:** `gcu_domain_service.Data.DspData`

Container object for all DSP-related configuration data. Groups DSP devices and their associated audio channels together within the root [`DataContainer`](DataContainer.md).

---

## Table of Contents

**Properties**
- [Dsps](#dsps)
- [Channels](#channels)

---

## Properties

### Dsps

```csharp
public List<Dsp> Dsps { get; set; }
```

**Type:** `List<`[`Dsp`](Dsp.md)`>`

Collection of DSP device configurations. Defaults to an empty list.

---

### Channels

```csharp
public List<Channel> Channels { get; set; }
```

**Type:** `List<`[`Channel`](Channel.md)`>`

Collection of audio channel configurations across all DSPs. Defaults to an empty list.
