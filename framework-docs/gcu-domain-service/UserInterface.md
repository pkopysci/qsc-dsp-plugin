# UserInterface

**Namespace:** `gcu_domain_service.Data.UserInterfaceData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single user interface panel (e.g., a Crestron touch panel). Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [IpId](#ipid)
- [Sgd](#sgd)
- [DefaultActivity](#defaultactivity)
- [Menu](#menu)
- [Tags](#tags)
- [ClassName](#classname)
- [Library](#library)

---

## Properties

### IpId

```csharp
public int IpId { get; set; }
```

**Type:** `int`

Gets or sets the IP-ID used to connect to the user interface. This is an integer representation of a hex value. Defaults to `0`.

---

### Sgd

```csharp
public string Sgd { get; set; }
```

**Type:** `string`

The smart graphics data (SGD) library file name needed if the UI is a VTPro-e based project. Defaults to `string.Empty`.

---

### DefaultActivity

```csharp
public string DefaultActivity { get; set; }
```

**Type:** `string`

Gets or sets the default activity to present when the system enters the active state. Defaults to `string.Empty`.

---

### Menu

```csharp
public List<MenuItem> Menu { get; set; }
```

**Type:** `List<`[`MenuItem`](MenuItem.md)`>`

Gets or sets the collection of main menu items to display on this UI. Defaults to an empty list.

---

### Tags

```csharp
public List<string> Tags { get; set; }
```

**Type:** `List<string>`

Collection of tags that can define special behavior for this UI. Defaults to an empty list.

---

### ClassName

```csharp
public string ClassName { get; set; }
```

**Type:** `string`

The fully qualified class name of the UI driver or implementation to load for this interface. Defaults to `string.Empty`.

---

### Library

```csharp
public string Library { get; set; }
```

**Type:** `string`

The DLL file name containing the driver or implementation class for this UI. Defaults to `string.Empty`.
