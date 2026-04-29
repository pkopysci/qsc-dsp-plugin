# MenuItemDataContainer

**Namespace:** `gcu_application_service.UserInterface`

**Inherits:** [`InfoContainer`](InfoContainer.md)

Data object for sending UI menu items to subscribers.

---

## Table of Contents

**Constructors**
- [MenuItemDataContainer(...)](#menuitemdatacontainer-1)

**Properties**
- [Control](#control)
- [SourceSelect](#sourceselect)

---

## Constructors

### MenuItemDataContainer(...)

```csharp
public MenuItemDataContainer(string id, string label, string icon, string control, string sourceSelect, List<string> tags)
```

Instantiates a new instance of `MenuItemDataContainer`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the menu item. Used for internal referencing. |
| `label` | `string` | The user-friendly name of the menu item. |
| `icon` | `string` | The image tag used for referencing the UI icon. |
| `control` | `string` | The activity associated with this menu item. |
| `sourceSelect` | `string` | The source ID to route on selection. Deprecated — pass the empty string `""`. |
| `tags` | `List<string>` | Collection of behavior tags. Values are determined by the interface plugin implementation. |

---

## Properties

### Control

```csharp
public string Control { get; }
```

The device controls or activity to display when this menu item is selected.

---

### SourceSelect

```csharp
public string SourceSelect { get; }
```

If associated with an input to route, this is the ID of the source to send to all destinations. **Deprecated** — use the routing API instead.
