# DisplayInfoContainer

**Namespace:** `gcu_application_service.DisplayControl`

**Inherits:** [`InfoContainer`](InfoContainer.md)

Data object used for sending display information to subscribers.

---

## Table of Contents

**Constructors**
- [DisplayInfoContainer(...)](#displayinfocontainer-1)

**Properties**
- [HasScreen](#hasscreen)
- [SupportsChannelControls](#supportschannelcontrols)
- [Inputs](#inputs)

---

## Constructors

### DisplayInfoContainer(...)

```csharp
public DisplayInfoContainer(string id, string label, string icon, List<string> tags, bool hasScreen, bool isOnline = false)
```

Instantiates a new instance of `DisplayInfoContainer`.

**Parameters**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `id` | `string` | | The unique ID of the display. Used for internal referencing. |
| `label` | `string` | | The user-friendly name of the display. |
| `icon` | `string` | | The image tag used for referencing the UI icon. |
| `tags` | `List<string>` | | A collection of custom tags used by the subscribed service. |
| `hasScreen` | `bool` | | `true` = there is a screen associated with this display. |
| `isOnline` | `bool` | `false` | `true` = device is online; `false` = device is offline. |

---

## Properties

### HasScreen

```csharp
public bool HasScreen { get; }
```

`true` if this display has an associated relay-controlled screen.

---

### SupportsChannelControls

```csharp
public bool SupportsChannelControls { get; init; }
```

`true` if the display supports onboard channel up/down controls.

---

### Inputs

```csharp
public List<DisplayInput> Inputs { get; set; }
```

A collection of all selectable inputs on the display.
