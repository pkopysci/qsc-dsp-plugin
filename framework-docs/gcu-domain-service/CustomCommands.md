# CustomCommands

**Namespace:** `gcu_domain_service.Data.DisplayData`

Custom command strings for display-specific freeze control operations. These override the standard driver commands when the display requires non-standard protocol strings.

---

## Table of Contents

**Properties**
- [FreezeOnTx](#freezeontx)
- [FreezeOnRx](#freezeonrx)
- [FreezeOffTx](#freezeofftx)
- [FreezeOffRx](#freezeoffrx)

---

## Properties

### FreezeOnTx

```csharp
public string FreezeOnTx { get; set; }
```

**Type:** `string`

The command string to transmit to the display to enable freeze (image freeze). Defaults to `string.Empty`.

---

### FreezeOnRx

```csharp
public string FreezeOnRx { get; set; }
```

**Type:** `string`

The expected response string received from the display when freeze is enabled. Defaults to `string.Empty`.

---

### FreezeOffTx

```csharp
public string FreezeOffTx { get; set; }
```

**Type:** `string`

The command string to transmit to the display to disable freeze. Defaults to `string.Empty`.

---

### FreezeOffRx

```csharp
public string FreezeOffRx { get; set; }
```

**Type:** `string`

The expected response string received from the display when freeze is disabled. Defaults to `string.Empty`.
