# ICrestronUserInterface

**Namespace:** `gcu_ui_service.Interfaces`

Events, methods, and properties for a user interface that requires access to the root Crestron control system and connection IP-ID. Implement this interface in addition to [`IUserInterface`](IUserInterface.md) when a plugin needs direct access to the `CrestronControlSystem` entry point object.

---

## Table of Contents

**Methods**
- [SetCrestronControl(CrestronControlSystem control)](#setcrestroncontrolcrestroncontrolsystem-control)

---

## Methods

### SetCrestronControl(CrestronControlSystem control)

```csharp
public void SetCrestronControl(CrestronControlSystem control)
```

Sets the plugin connection information by providing access to the root Crestron control system object.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `control` | `CrestronControlSystem` | The root entry point object used to establish a control connection. |
