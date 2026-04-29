# IIrPort

**Namespace:** `gcu_common_utils.IrComs`

Interface defining the minimum events, properties, and methods required for any communication implementation that uses IR (infrared) control.

---

## Table of Contents

**Events**
- [EnableStatusChanged](#enablestatuschanged)

**Properties**
- [IsRegistered](#isregistered)
- [Id](#id)
- [IsEnabled](#isenabled)

**Methods**
- [Enable()](#enable)
- [Disable()](#disable)
- [Send(string command, int pulseLength)](#sendstring-command-int-pulselength)
- [Register()](#register)
- [LoadIrDriver(string filePath)](#loadirdriverstring-filepath)

---

## Events

### EnableStatusChanged

```csharp
event EventHandler? EnableStatusChanged
```

Event triggered whenever the IR port enable status changes. Typically triggered by a call to `Enable()` or `Disable()`.

---

## Properties

### IsRegistered

```csharp
bool IsRegistered { get; }
```

`true` = the underlying IR control has completed its initialization/registration process; `false` = not yet registered.

---

### Id

```csharp
string Id { get; }
```

The unique ID of the port used for internal referencing.

---

### IsEnabled

```csharp
bool IsEnabled { get; }
```

`true` = the IR port is able to send commands; `false` = the device will not send commands.

---

## Methods

### Enable()

```csharp
void Enable()
```

Sets `IsEnabled` to `true` and allows commands to be sent through the transmitter. `EnableStatusChanged` will also be triggered.

---

### Disable()

```csharp
void Disable()
```

Sets `IsEnabled` to `false` and prevents commands from being sent through the transmitter. `EnableStatusChanged` will also be triggered.

---

### Send(string command, int pulseLength)

```csharp
void Send(string command, int pulseLength)
```

Send a command through the IR transmitter for the given length of time.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `command` | `string` | The IR command to send. |
| `pulseLength` | `int` | The length of the pulse in milliseconds. |

---

### Register()

```csharp
void Register()
```

Runs any internal registration required by the IR port implementation. This must set `IsRegistered` to `true` on success.

---

### LoadIrDriver(string filePath)

```csharp
void LoadIrDriver(string filePath)
```

Loads the IR configuration to the underlying IR port if it uses one, such as a Crestron `IrOutputPort` object.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `filePath` | `string` | The full path to the IR configuration file. |
