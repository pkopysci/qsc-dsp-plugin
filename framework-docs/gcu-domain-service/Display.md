# Display

**Namespace:** `gcu_domain_service.Data.DisplayData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single display device. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Label](#label)
- [Icon](#icon)
- [Tags](#tags)
- [HasScreen](#hasscreen)
- [RelayController](#relaycontroller)
- [ScreenUpRelay](#screenuprelay)
- [ScreenDownRelay](#screendownrelay)
- [LecternInput](#lecterninput)
- [StationInput](#stationinput)
- [Connection](#connection)
- [UserAttributes](#userattributes)
- [CustomCommands](#customcommands)

---

## Properties

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name shown in UI elements. Defaults to `string.Empty`.

---

### Icon

```csharp
public string Icon { get; set; }
```

**Type:** `string`

Icon identifier used to represent this display in the UI. Defaults to `string.Empty`.

---

### Tags

```csharp
public List<string> Tags { get; set; }
```

**Type:** `List<string>`

A collection of string tags used to define or filter special behaviors for this display. Defaults to an empty list.

---

### HasScreen

```csharp
public bool HasScreen { get; set; }
```

**Type:** `bool`

Indicates whether this display is paired with a motorized projection screen. When `true`, the `RelayController`, `ScreenUpRelay`, and `ScreenDownRelay` properties are used. Defaults to `false`.

---

### RelayController

```csharp
public string RelayController { get; set; }
```

**Type:** `string`

The ID of the endpoint or control system that owns the relays used to control the projection screen. Defaults to `string.Empty`.

---

### ScreenUpRelay

```csharp
public int ScreenUpRelay { get; set; }
```

**Type:** `int`

The relay index used to raise the projection screen. Defaults to `0`.

---

### ScreenDownRelay

```csharp
public int ScreenDownRelay { get; set; }
```

**Type:** `int`

The relay index used to lower the projection screen. Defaults to `0`.

---

### LecternInput

```csharp
public int LecternInput { get; set; }
```

**Type:** `int`

The input index on this display that corresponds to the lectern connection. Defaults to `0`.

---

### StationInput

```csharp
public int StationInput { get; set; }
```

**Type:** `int`

The input index on this display that corresponds to the station connection. Defaults to `0`.

---

### Connection

```csharp
public Connection Connection { get; set; }
```

**Type:** [`Connection`](Connection.md)

The connection configuration (transport, host, port, credentials) used to control the display. Defaults to an empty `Connection` instance.

---

### UserAttributes

```csharp
public List<UserAttribute> UserAttributes { get; set; }
```

**Type:** `List<`[`UserAttribute`](UserAttribute.md)`>`

Collection of driver-specific configuration attributes for this display. Defaults to an empty list.

---

### CustomCommands

```csharp
public CustomCommands CustomCommands { get; set; }
```

**Type:** [`CustomCommands`](CustomCommands.md)

Custom protocol command strings for display-specific operations such as freeze. Defaults to an empty `CustomCommands` instance.
