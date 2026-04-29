# Logic

**Namespace:** `gcu_domain_service.Data.RoomInfoData`

Configuration data specifying the application and presentation service plug-ins to load for the room. If these values are empty, the system will select a default service based on the `SystemType` property in [`RoomInfo`](RoomInfo.md).

---

## Table of Contents

**Properties**
- [AppServiceLibrary](#appservicelibrary)
- [AppServiceClass](#appserviceclass)
- [PresentationServiceLibrary](#presentationservicelibrary)
- [PresentationServiceClass](#presentationserviceclass)

---

## Properties

### AppServiceLibrary

```csharp
public string AppServiceLibrary { get; set; }
```

**Type:** `string`

The DLL file name of the application service plug-in to load for this room. Defaults to `string.Empty`.

---

### AppServiceClass

```csharp
public string AppServiceClass { get; set; }
```

**Type:** `string`

The fully qualified class name of the application service to instantiate from `AppServiceLibrary`. Defaults to `string.Empty`.

---

### PresentationServiceLibrary

```csharp
public string PresentationServiceLibrary { get; set; }
```

**Type:** `string`

The DLL file name of the presentation service plug-in to load for this room. Defaults to `string.Empty`.

---

### PresentationServiceClass

```csharp
public string PresentationServiceClass { get; set; }
```

**Type:** `string`

The fully qualified class name of the presentation service to instantiate from `PresentationServiceLibrary`. Defaults to `string.Empty`.
