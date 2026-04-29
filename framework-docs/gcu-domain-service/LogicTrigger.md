# LogicTrigger

**Namespace:** `gcu_domain_service.Data.DspData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a DSP logic trigger. Logic triggers are named control tags in a DSP that can fire discrete events (e.g., muting all zones, enabling/disabling outputs). Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Label](#label)
- [TagName](#tagname)
- [Tags](#tags)

---

## Properties

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name for this logic trigger, used in UI or logging. Defaults to `string.Empty`.

---

### TagName

```csharp
public string TagName { get; set; }
```

**Type:** `string`

The DSP control tag name that activates this logic trigger. Defaults to `string.Empty`.

---

### Tags

```csharp
public List<string> Tags { get; set; }
```

**Type:** `List<string>`

A collection of string tags used to define or filter special behaviors for this trigger. Defaults to an empty list.
