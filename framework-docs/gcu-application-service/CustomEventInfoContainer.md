# CustomEventInfoContainer

**Namespace:** `gcu_application_service.CustomEvents`

**Inherits:** [`InfoContainer`](InfoContainer.md)

Data container for a single custom event.

---

## Table of Contents

**Constructors**
- [CustomEventInfoContainer(...)](#customeventinfocontainer-1)

**Properties**
- [IsActive](#isactive)

---

## Constructors

### CustomEventInfoContainer(...)

```csharp
public CustomEventInfoContainer(
    string id,
    string label,
    string icon,
    List<string> tags,
    bool isOnline = false,
    bool isActive = false)
```

**Parameters**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `id` | `string` | | ID of the event used for internal referencing. |
| `label` | `string` | | Human-friendly name of the event. |
| `icon` | `string` | | Icon tag used by a user interface. |
| `tags` | `List<string>` | | Collection of behavior flags that may be used by the UI or application service. |
| `isOnline` | `bool` | `false` | `true` = the device associated with the event is connected; `false` otherwise. |
| `isActive` | `bool` | `false` | `true` = this event is currently active/in use; `false` = not selected. |

---

## Properties

### IsActive

```csharp
public bool IsActive { get; set; }
```

`true` if this event is currently active/selected; `false` otherwise.
