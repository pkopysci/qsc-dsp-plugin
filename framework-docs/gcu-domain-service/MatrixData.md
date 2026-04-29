# MatrixData

**Namespace:** `gcu_domain_service.Data.RoutingData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single AV matrix switcher. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Label](#label)
- [ClassName](#classname)
- [Inputs](#inputs)
- [Outputs](#outputs)
- [Connection](#connection)

---

## Properties

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Display name for the matrix switcher, used in UI and logging. Defaults to `string.Empty`.

---

### ClassName

```csharp
public string ClassName { get; set; }
```

**Type:** `string`

The fully qualified class name of the driver or control implementation to load for this matrix. Defaults to `string.Empty`.

---

### Inputs

```csharp
public int Inputs { get; set; }
```

**Type:** `int`

The total number of input ports on the matrix switcher. Defaults to `0`.

---

### Outputs

```csharp
public int Outputs { get; set; }
```

**Type:** `int`

The total number of output ports on the matrix switcher. Defaults to `0`.

---

### Connection

```csharp
public Connection Connection { get; set; }
```

**Type:** [`Connection`](Connection.md)

The connection configuration (transport, host, port, credentials) used to control the matrix switcher. Defaults to an empty `Connection` instance.
