# BaseData

**Namespace:** `gcu_domain_service.Data`

Base data object for all configuration items. All device and configuration data classes in the `gcu_domain_service.Data` namespace inherit from this class.

---

## Table of Contents

**Properties**
- [Id](#id)
- [Manufacturer](#manufacturer)
- [Model](#model)

---

## Properties

### Id

```csharp
public string Id { get; set; }
```

Gets or sets a unique identifier for the data object. Used to reference the information during runtime. Defaults to `string.Empty`.

---

### Manufacturer

```csharp
public string Manufacturer { get; set; }
```

Gets or sets the hardware manufacturer, if relevant. Defaults to `string.Empty`.

---

### Model

```csharp
public string Model { get; set; }
```

Gets or sets the device name as defined by the manufacturer. Defaults to `string.Empty`.
