# Dsp

**Namespace:** `gcu_domain_service.Data.DspData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single DSP (Digital Signal Processor) device. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [CoreId](#coreid)
- [Dependencies](#dependencies)
- [Connection](#connection)
- [Presets](#presets)
- [LogicTriggers](#logictriggers)

---

## Properties

### CoreId

```csharp
public int CoreId { get; set; }
```

**Type:** `int`

The core or slot identifier for the DSP within the control system. Defaults to `0`.

---

### Dependencies

```csharp
public List<string> Dependencies { get; set; }
```

**Type:** `List<string>`

A list of dependency file names (DLLs or drivers) that must be loaded for this DSP to function. Defaults to an empty list.

---

### Connection

```csharp
public Connection Connection { get; set; }
```

**Type:** [`Connection`](Connection.md)

The connection configuration (transport, host, port, credentials) used to control the DSP. Defaults to an empty `Connection` instance.

---

### Presets

```csharp
public List<Preset> Presets { get; set; }
```

**Type:** `List<`[`Preset`](DspPreset.md)`>`

Collection of DSP presets (scene recalls) configured for this device. Defaults to an empty list.

---

### LogicTriggers

```csharp
public List<LogicTrigger> LogicTriggers { get; set; }
```

**Type:** `List<`[`LogicTrigger`](LogicTrigger.md)`>`

Collection of logic trigger tags associated with this DSP. Defaults to an empty list.
