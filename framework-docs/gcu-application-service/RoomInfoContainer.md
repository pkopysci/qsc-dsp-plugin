# RoomInfoContainer

**Namespace:** `gcu_application_service.Base`

**Inherits:** [`InfoContainer`](InfoContainer.md)

Data object containing general information about the controlled location (room or space).

---

## Table of Contents

**Constructors**
- [RoomInfoContainer(...)](#roominfocontainer-1)

**Properties**
- [HelpContact](#helpcontact)
- [SystemType](#systemtype)
- [PresentationServiceLibrary](#presentationservicelibrary)
- [PresentationServiceClass](#presentationserviceclass)

---

## Constructors

### RoomInfoContainer(...)

```csharp
public RoomInfoContainer(string id, string label, string helpContact, string systemType)
```

Instantiates a new instance of `RoomInfoContainer`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the room as set in the configuration. |
| `label` | `string` | The user-friendly room name. |
| `helpContact` | `string` | The contact information to use when contacting tech support. |
| `systemType` | `string` | The system behavior identifier set in the configuration file. |

---

## Properties

### HelpContact

```csharp
public string HelpContact { get; }
```

The help contact information that was set in the room configuration (e.g., phone number or email).

---

### SystemType

```csharp
public string SystemType { get; }
```

The system type identifier for this room, as set in the configuration file.

---

### PresentationServiceLibrary

```csharp
public string PresentationServiceLibrary { get; init; }
```

The custom presentation service plugin library that will be loaded at system boot.

---

### PresentationServiceClass

```csharp
public string PresentationServiceClass { get; init; }
```

The class name of the custom presentation service plugin that will be instantiated from `PresentationServiceLibrary`.
