# RoomInfo

**Namespace:** `gcu_domain_service.Data.RoomInfoData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration item for setting the basic room information in the system. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [RoomName](#roomname)
- [HelpContact](#helpcontact)
- [SystemType](#systemtype)
- [ShutdownHour](#shutdownhour)
- [ShutdownMinute](#shutdownminute)
- [Logic](#logic)

---

## Properties

### RoomName

```csharp
public string RoomName { get; set; }
```

**Type:** `string`

Gets or sets the name or number of the room from the configuration. Defaults to `string.Empty`.

---

### HelpContact

```csharp
public string HelpContact { get; set; }
```

**Type:** `string`

Gets or sets the phone number or other support contact information displayed in the UI. Defaults to `string.Empty`.

---

### SystemType

```csharp
public string SystemType { get; set; }
```

**Type:** `string`

Defines what system behavior is expected for this room. Typical values: `baseline`, `active`, `lecture`. Defaults to `string.Empty`.

---

### ShutdownHour

```csharp
public int ShutdownHour { get; set; }
```

**Type:** `int`

The hour (0–23) at which the system should automatically shut down. Defaults to `0`.

---

### ShutdownMinute

```csharp
public int ShutdownMinute { get; set; }
```

**Type:** `int`

The minute (0–59) at which the system should automatically shut down. Defaults to `0`.

---

### Logic

```csharp
public Logic Logic { get; set; }
```

**Type:** [`Logic`](Logic.md)

The application and presentation service plug-in configuration used to drive the room. If this is empty, the default service related to `SystemType` will be loaded. Defaults to an empty `Logic` instance.
