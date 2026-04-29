# FusionInfo

**Namespace:** `gcu_domain_service.Data.FusionData`

**Inherits:** [`BaseData`](BaseData.md)

Crestron Fusion room monitoring configuration data. Contains the parameters needed to register the control system room with a Crestron Fusion server. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [RoomName](#roomname)
- [GUID](#guid)
- [IpId](#ipid)

---

## Properties

### RoomName

```csharp
public string RoomName { get; set; }
```

**Type:** `string`

The display name of the room as it should appear in the Crestron Fusion server. Defaults to `string.Empty`.

---

### GUID

```csharp
public string GUID { get; set; }
```

**Type:** `string`

The globally unique identifier (GUID) assigned to this room in Crestron Fusion. Defaults to `string.Empty`.

---

### IpId

```csharp
public int IpId { get; set; }
```

**Type:** `int`

The IP-ID (integer representation of a hex value) used to connect to the Crestron Fusion server. Defaults to `0`.
