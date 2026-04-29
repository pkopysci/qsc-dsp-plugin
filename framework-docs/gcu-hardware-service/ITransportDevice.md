# ITransportDevice

**Namespace:** `gcu_hardware_service.TransportDevices`

**Implements:** [`IBaseDevice`](IBaseDevice.md)

Interface to be implemented by any class that uses transport commands (Blu-ray, DVD player, etc.). Provides core navigation and power controls common to all transport devices.

---

## Table of Contents

**Properties**
- [SupportsColorButtons](#supportscolorbuttons)
- [SupportsDiscretePower](#supportsdiscretepower)

**Methods**
- [Initialize(string id, string label)](#initializestring-id-string-label)
- [PowerOn()](#poweron)
- [PowerOff()](#poweroff)
- [PowerToggle()](#powertoggle)
- [Guide()](#guide)
- [Info()](#info)
- [Exit()](#exit)
- [Back()](#back)
- [NavUp()](#navup)
- [NavDown()](#navdown)
- [NavLeft()](#navleft)
- [NavRight()](#navright)
- [Select()](#select)

---

## Properties

### SupportsColorButtons

```csharp
bool SupportsColorButtons { get; }
```

`true` if the device supports color button commands (red, green, yellow, blue); otherwise `false`.

---

### SupportsDiscretePower

```csharp
bool SupportsDiscretePower { get; }
```

`true` if the device supports discrete power on and off commands; otherwise `false`.

---

## Methods

### Initialize(string id, string label)

```csharp
void Initialize(string id, string label)
```

Configure the device's identifying information.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | A unique ID used to reference this device. |
| `label` | `string` | A human-friendly name of this device. |

---

### PowerOn()

```csharp
void PowerOn()
```

Send a power on command to the device.

---

### PowerOff()

```csharp
void PowerOff()
```

Send a power off command to the device.

---

### PowerToggle()

```csharp
void PowerToggle()
```

Send a power toggle command to the device.

---

### Guide()

```csharp
void Guide()
```

Send a guide button command to the device.

---

### Info()

```csharp
void Info()
```

Send an info button command to the device.

---

### Exit()

```csharp
void Exit()
```

Send an exit button command to the device.

---

### Back()

```csharp
void Back()
```

Send a back button command to the device.

---

### NavUp()

```csharp
void NavUp()
```

Send a navigate up command to the device.

---

### NavDown()

```csharp
void NavDown()
```

Send a navigate down command to the device.

---

### NavLeft()

```csharp
void NavLeft()
```

Send a navigate left command to the device.

---

### NavRight()

```csharp
void NavRight()
```

Send a navigate right command to the device.

---

### Select()

```csharp
void Select()
```

Send a select/enter command to the device.
