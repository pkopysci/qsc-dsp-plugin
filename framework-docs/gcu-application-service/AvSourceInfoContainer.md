# AvSourceInfoContainer

**Namespace:** `gcu_application_service.AvRouting`

**Inherits:** [`InfoContainer`](InfoContainer.md)

Container object for sending information about a single audio/video source to subscribers.

---

## Table of Contents

**Properties**
- [Empty](#empty)
- [ControlId](#controlid)
- [SupportSync](#supportsync)
- [HasSync](#hassync)

**Constructors**
- [AvSourceInfoContainer(...)](#avsourceinfocontainer-1)

---

## Properties

### Empty

```csharp
public new static readonly AvSourceInfoContainer Empty
```

Default/empty AV source. Used when a source query does not find a match.

---

### ControlId

```csharp
public string ControlId { get; }
```

The unique ID of the transport device (e.g., cable box, Blu-ray) associated with this source, if one exists. Empty string if no device is associated.

---

### SupportSync

```csharp
public bool SupportSync { get; init; }
```

`true` if the AVR associated with this source supports video sync detection.

---

### HasSync

```csharp
public bool HasSync { get; init; }
```

`true` if sync is detected; `false` if no sync is detected or the device does not support sync detection.

---

## Constructors

### AvSourceInfoContainer(...)

```csharp
public AvSourceInfoContainer(string id, string label, string icon, List<string> tags, string controlId)
```

Creates a new instance of `AvSourceInfoContainer`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the source. Used for internal referencing. |
| `label` | `string` | The user-friendly name of the source. |
| `icon` | `string` | The image tag used for referencing the UI icon. |
| `tags` | `List<string>` | A collection of custom tags used by the subscribed service. |
| `controlId` | `string` | The unique ID of the transport device associated with the source, if any. |
