# Channel

**Namespace:** `gcu_domain_service.Data.DspData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single DSP audio channel. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [LevelControlTag](#levelcontroltag)
- [MuteControlTag](#mutecontroltag)
- [RouterControlTag](#routercontroltag)
- [DspId](#dspid)
- [RouterIndex](#routerindex)
- [BankIndex](#bankindex)
- [Label](#label)
- [Icon](#icon)
- [LevelMax](#levelmax)
- [LevelMin](#levelmin)
- [ZoneEnableToggles](#zoneenabletoggles)
- [Tags](#tags)

---

## Properties

### LevelControlTag

```csharp
public string LevelControlTag { get; set; }
```

**Type:** `string`

The DSP control tag used to set the volume level for this channel. Defaults to `string.Empty`.

---

### MuteControlTag

```csharp
public string MuteControlTag { get; set; }
```

**Type:** `string`

The DSP control tag used to mute or unmute this channel. Defaults to `string.Empty`.

---

### RouterControlTag

```csharp
public string RouterControlTag { get; set; }
```

**Type:** `string`

The DSP control tag used to route audio through a matrix or router for this channel. Defaults to `string.Empty`.

---

### DspId

```csharp
public string DspId { get; set; }
```

**Type:** `string`

The `Id` of the [`Dsp`](Dsp.md) device that owns this channel. Defaults to `string.Empty`.

---

### RouterIndex

```csharp
public int RouterIndex { get; set; }
```

**Type:** `int`

The router or crosspoint index for this channel within the DSP. Defaults to `0`.

---

### BankIndex

```csharp
public int BankIndex { get; set; }
```

**Type:** `int`

The bank or fader index of this channel within the DSP. Defaults to `0`.

---

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name for this channel, used in UI elements. Defaults to `string.Empty`.

---

### Icon

```csharp
public string Icon { get; set; }
```

**Type:** `string`

Icon identifier used to represent this channel in the UI. Defaults to `string.Empty`.

---

### LevelMax

```csharp
public int LevelMax { get; set; }
```

**Type:** `int`

The maximum volume level value for this channel. Defaults to `0`.

---

### LevelMin

```csharp
public int LevelMin { get; set; }
```

**Type:** `int`

The minimum volume level value for this channel. Defaults to `0`.

---

### ZoneEnableToggles

```csharp
public List<ZoneEnableToggle> ZoneEnableToggles { get; set; }
```

**Type:** `List<`[`ZoneEnableToggle`](ZoneEnableToggle.md)`>`

Collection of zone enable/disable toggle controls associated with this channel. Defaults to an empty list.

---

### Tags

```csharp
public List<string> Tags { get; set; }
```

**Type:** `List<string>`

A collection of string tags used to define or filter special behaviors for this channel. Defaults to an empty list.
