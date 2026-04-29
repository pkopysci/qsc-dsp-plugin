# TransportFavorite

**Namespace:** `gcu_domain_service.Data.TransportDeviceData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single favorite channel entry on a transport device such as a cable or satellite box. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Label](#label)
- [Number](#number)

---

## Properties

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name for the favorite channel, used in UI elements. Defaults to `string.Empty`.

---

### Number

```csharp
public string Number { get; set; }
```

**Type:** `string`

The channel number or code to tune to when this favorite is selected. Stored as a string to support leading zeros and multi-digit codes. Defaults to `string.Empty`.
