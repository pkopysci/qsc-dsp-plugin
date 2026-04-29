# ZoneEnableToggle

**Namespace:** `gcu_domain_service.Data.DspData`

Configuration data for a DSP zone enable/disable toggle. Represents a named control tag that toggles a zone on or off within a DSP audio channel.

---

## Table of Contents

**Properties**
- [ZoneId](#zoneid)
- [Label](#label)
- [Tag](#tag)

---

## Properties

### ZoneId

```csharp
public string ZoneId { get; set; }
```

**Type:** `string`

The unique identifier for the zone that this toggle controls. Defaults to `string.Empty`.

---

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name for this toggle, used in UI elements. Defaults to `string.Empty`.

---

### Tag

```csharp
public string Tag { get; set; }
```

**Type:** `string`

The DSP control tag name used to enable or disable this zone. Defaults to `string.Empty`.
