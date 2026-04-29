# ICrestronDevice

**Namespace:** `gcu_hardware_service`

Required methods for implementing a hardware plugin that requires direct hooks into the root control system object.

---

## Table of Contents

**Methods**
- [SetControlSystem(CrestronControlSystem controlSystem)](#setcontrolsystemcrestroncontrolsystem-controlsystem)

---

## Methods

### SetControlSystem(CrestronControlSystem controlSystem)

```csharp
void SetControlSystem(CrestronControlSystem controlSystem)
```

Assign a Crestron control system to the device control plugin.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `controlSystem` | `CrestronControlSystem` | The root control system object. |
