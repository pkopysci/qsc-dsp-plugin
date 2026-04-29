# AudioChannelInfoContainer

**Namespace:** `gcu_application_service.AudioControl`

**Inherits:** [`InfoContainer`](InfoContainer.md)

Data object for sending information about a single audio channel in the system to subscribers.

---

## Table of Contents

**Constructors**
- [AudioChannelInfoContainer(...)](#audiochannelinfocontainer-1)

**Properties**
- [ZoneEnableControls](#zoneenablecontrols)

---

## Constructors

### AudioChannelInfoContainer(...)

```csharp
public AudioChannelInfoContainer(
    string id,
    string label,
    string icon,
    List<string> tags,
    List<InfoContainer> zoneEnables)
```

Creates a new instance of `AudioChannelInfoContainer`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the channel. Used for internal referencing. |
| `label` | `string` | The user-friendly name of the channel. |
| `icon` | `string` | The image tag used for referencing the UI icon. |
| `tags` | `List<string>` | A collection of custom tags used by the subscribed service. |
| `zoneEnables` | `List<InfoContainer>` | A collection of data objects defining what audio zones this channel can be routed to. |

---

## Properties

### ZoneEnableControls

```csharp
public List<InfoContainer> ZoneEnableControls { get; }
```

A collection of audio zone data objects that define where this channel can be routed.
