# DisplayInput

**Namespace:** `gcu_application_service.DisplayControl`

Data object representing a single input on a display or projector device.

---

## Table of Contents

**Properties**
- [Id](#id)
- [Label](#label)
- [InputNumber](#inputnumber)
- [Tags](#tags)

---

## Properties

### Id

```csharp
public string Id { get; set; }
```

The ID of the input, used for internal referencing. Defaults to `"DI-DEFAULT"`.

---

### Label

```csharp
public string Label { get; set; }
```

Human-friendly name of the input. Defaults to `"Display Input"`.

---

### InputNumber

```csharp
public int InputNumber { get; set; }
```

The input index on the display device for this input.

---

### Tags

```csharp
public List<string> Tags { get; set; }
```

Collection of functional tags associated with this input. These tags can be used by the application service or user interface implementation.
