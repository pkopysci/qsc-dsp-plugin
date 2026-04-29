# Preset (DspData)

**Namespace:** `gcu_domain_service.Data.DspData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single DSP preset (scene recall). Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Bank](#bank)
- [Index](#index)
- [Tags](#tags)

---

## Properties

### Bank

```csharp
public string Bank { get; set; }
```

**Type:** `string`

The name or identifier of the DSP preset bank this preset belongs to. Defaults to `string.Empty`.

---

### Index

```csharp
public int Index { get; set; }
```

**Type:** `int`

The numeric index of this preset within its bank. Used to recall the preset on the DSP. Defaults to `0`.

---

### Tags

```csharp
public List<string> Tags
```

**Type:** `List<string>`

A collection of string tags used to define or filter special behaviors for this preset. Defaults to an empty list.

> **Note:** `Tags` is a public field (not a property) in the current implementation.
