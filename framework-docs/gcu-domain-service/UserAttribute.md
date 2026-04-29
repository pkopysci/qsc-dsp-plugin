# UserAttribute

**Namespace:** `gcu_domain_service.Data.DriverData`

**Inherits:** [`BaseData`](BaseData.md)

A driver configuration attribute for a TCP or serial connection. Used to pass driver-specific configuration values that fall outside the standard connection properties. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

Valid `DataType` values: `String`, `Number`, `Hex`, `Boolean`.

---

## Table of Contents

**Properties**
- [DataType](#datatype)
- [Value](#value)

---

## Properties

### DataType

```csharp
public string DataType { get; set; }
```

**Type:** `string`

The data type of the attribute value. Valid values: `String`, `Number`, `Hex`, `Boolean`. Defaults to `string.Empty`.

---

### Value

```csharp
public string Value { get; set; }
```

**Type:** `string`

The string representation of the attribute value. Interpretation depends on `DataType`. Defaults to `string.Empty`.
