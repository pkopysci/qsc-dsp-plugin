# Endpoint

**Namespace:** `gcu_domain_service.Data.EndpointData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single AV endpoint device (e.g., a remote switcher, extender, or gateway that hosts additional serial, IR, and relay ports). Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Port](#port)
- [Host](#host)
- [Library](#library)
- [Class](#class)
- [Relays](#relays)
- [Comports](#comports)
- [IrPorts](#irports)

---

## Properties

### Port

```csharp
public int Port { get; set; }
```

**Type:** `int`

The TCP/IP port number used to connect to the endpoint. Defaults to `0`.

---

### Host

```csharp
public string Host { get; set; }
```

**Type:** `string`

The IP address or hostname of the endpoint device. Defaults to `string.Empty`.

---

### Library

```csharp
public string Library { get; set; }
```

**Type:** `string`

The DLL file name containing the driver or implementation class for this endpoint. Defaults to `string.Empty`.

---

### Class

```csharp
public string Class { get; set; }
```

**Type:** `string`

The fully qualified class name of the driver or implementation to load for this endpoint. Defaults to `string.Empty`.

---

### Relays

```csharp
public int[] Relays { get; set; }
```

**Type:** `int[]`

Array of relay port numbers available on this endpoint. Defaults to an empty array.

---

### Comports

```csharp
public int[] Comports { get; set; }
```

**Type:** `int[]`

Array of serial (COM) port numbers available on this endpoint. Defaults to an empty array.

---

### IrPorts

```csharp
public int[] IrPorts { get; set; }
```

**Type:** `int[]`

Array of IR port numbers available on this endpoint. Defaults to an empty array.
