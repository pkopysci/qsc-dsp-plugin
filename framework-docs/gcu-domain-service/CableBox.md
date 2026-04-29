# CableBox

**Namespace:** `gcu_domain_service.Data.TransportDeviceData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single cable/satellite box device. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Label](#label)
- [Connection](#connection)
- [UserAttributes](#userattributes)
- [Favorites](#favorites)

---

## Properties

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name for the cable/satellite box, used in UI elements. Defaults to `string.Empty`.

---

### Connection

```csharp
public Connection Connection { get; set; }
```

**Type:** [`Connection`](Connection.md)

The connection configuration (transport, host, port, credentials) used to control the cable box. Defaults to an empty `Connection` instance.

---

### UserAttributes

```csharp
public List<UserAttribute> UserAttributes { get; set; }
```

**Type:** `List<`[`UserAttribute`](UserAttribute.md)`>`

Collection of driver-specific configuration attributes for this cable box. Defaults to an empty list.

---

### Favorites

```csharp
public List<TransportFavorite> Favorites { get; set; }
```

**Type:** `List<`[`TransportFavorite`](TransportFavorite.md)`>`

Collection of favorite channels configured for this cable box. Defaults to an empty list.
