# LightingItemInfoContainer

**Namespace:** `gcu_application_service.LightingControl`

**Inherits:** [`InfoContainer`](InfoContainer.md)

Data object representing a single lighting zone or scene item within a lighting controller.

---

## Table of Contents

**Constructors**
- [LightingItemInfoContainer(...)](#lightingiteminfocontainer-1)

**Properties**
- [Index](#index)

---

## Constructors

### LightingItemInfoContainer(...)

```csharp
public LightingItemInfoContainer(string id, string label, string icon, List<string> tags, int index)
```

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the item. Used for internal referencing. |
| `label` | `string` | The user-friendly name of the item. |
| `icon` | `string` | The image tag used for referencing the UI icon. |
| `tags` | `List<string>` | A collection of custom tags used by the subscribed service. |
| `index` | `int` | The 0-based index of this zone or scene on the device. |

---

## Properties

### Index

```csharp
public int Index { get; }
```

The 0-based index of this zone or scene on the lighting device.
