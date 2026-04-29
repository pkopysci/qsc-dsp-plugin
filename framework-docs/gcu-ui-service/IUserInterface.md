# IUserInterface

**Namespace:** `gcu_ui_service.Interfaces`

Required events, methods, and properties for implementing any user interface plugin in the GCU AV framework. Defines the full lifecycle for a UI plugin: configure with `SetUiData()`, prepare internal objects with `Initialize()`, then open the hardware connection with `Connect()`.

---

## Table of Contents

**Events**
- [OnlineStatusChanged](#onlinestatuschanged)

**Properties**
- [IsInitialized](#isinitialized)
- [IsOnline](#isonline)
- [IsXpanel](#isxpanel)
- [Id](#id)

**Methods**
- [SetUiData(UserInterfaceDataContainer uiData)](#setuidatauserinterfacedatacontainer-uidata)
- [Initialize()](#initialize)
- [Connect()](#connect)

---

## Events

### OnlineStatusChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> OnlineStatusChanged
```

Triggered whenever the connection to the underlying device changes. The event arg contains the ID of the interface that changed.

---

## Properties

### IsInitialized

```csharp
bool IsInitialized { get; }
```

Gets a value indicating whether the panel has been initialized and connected with the interface. `true` = initialized, `false` = not yet initialized.

---

### IsOnline

```csharp
bool IsOnline { get; }
```

Gets a value indicating whether the connected interface hardware is online with the control system. `true` = device is online, `false` = device is offline.

---

### IsXpanel

```csharp
bool IsXpanel { get; }
```

Gets a value indicating whether the user interface is an XPanel or other software-only support interface.

---

### Id

```csharp
string Id { get; }
```

The unique identifier used when searching for or referencing this device.

---

## Methods

### SetUiData(UserInterfaceDataContainer uiData)

```csharp
void SetUiData(UserInterfaceDataContainer uiData)
```

Prepare the interface for initialization by defining the general configuration. Must be called before `Initialize()`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `uiData` | `UserInterfaceDataContainer` | The configuration data object that represents the UI being created. |

---

### Initialize()

```csharp
void Initialize()
```

Call once all configuration information has been set. Prepares internal objects for connection. Must be called after `SetUiData()` and before `Connect()`.

---

### Connect()

```csharp
void Connect()
```

Call once all necessary data has been populated and `Initialize()` has been successfully called. Opens a connection with the interface hardware.
