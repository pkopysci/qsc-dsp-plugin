# Bluray

**Namespace:** `gcu_domain_service.Data.TransportDeviceData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single Blu-ray player device. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Label](#label)
- [Connection](#connection)
- [UserAttributes](#userattributes)

---

## Properties

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name for the Blu-ray player, used in UI elements. Defaults to `string.Empty`.

---

### Connection

```csharp
public Connection Connection { get; set; }
```

**Type:** [`Connection`](Connection.md)

The connection configuration (transport, host, port, credentials) used to control the Blu-ray player. Defaults to an empty `Connection` instance.

---

### UserAttributes

```csharp
public List<UserAttribute> UserAttributes { get; set; }
```

**Type:** `List<`[`UserAttribute`](UserAttribute.md)`>`

Collection of driver-specific configuration attributes for this Blu-ray player. Defaults to an empty list.
