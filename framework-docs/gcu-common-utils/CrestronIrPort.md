# CrestronIrPort

**Namespace:** `gcu_common_utils.IrComs`

**Implements:** [`IIrPort`](IIrPort.md)

Abstraction wrapper class for the Crestron `IROutputPort`. Provides a consistent interface for IR transmitter control on Crestron control systems.

**Constructor**

```csharp
public CrestronIrPort(string id, IROutputPort port)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | `string` | A unique identifier for this port instance. |
| `port` | `IROutputPort` | The underlying Crestron IR output port to wrap. |

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
public event EventHandler? EnableStatusChanged
```

Triggered whenever the IR port enable status changes. Raised by calls to `Enable()` or `Disable()`.

---

## Properties

### IsRegistered

```csharp
public bool IsRegistered { get; }
```

`true` = the underlying IR port has completed its registration process; `false` = not yet registered.

---

### Id

```csharp
public string Id { get; }
```

The unique identifier for this port instance, as provided at construction.

---

### IsEnabled

```csharp
public bool IsEnabled { get; }
```

`true` = the IR port is enabled and able to send commands; `false` = commands will not be sent.

---

## Methods

### Enable()

```csharp
public void Enable()
```

Sets `IsEnabled` to `true` and raises `EnableStatusChanged`.

---

### Disable()

```csharp
public void Disable()
```

Sets `IsEnabled` to `false` and raises `EnableStatusChanged`.

---

### Send(string command, int pulseLength)

```csharp
public void Send(string command, int pulseLength)
```

Send a command through the IR transmitter for the given length of time. Does nothing if the port is not registered or not enabled.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `command` | `string` | The IR command to send. |
| `pulseLength` | `int` | The length of the pulse in milliseconds. |

---

### Register()

```csharp
public void Register()
```

Marks the port as registered by setting `IsRegistered` to `true`. Crestron IR ports do not require explicit registration through this class — registration is handled on the controller endpoint.

---

### LoadIrDriver(string filePath)

```csharp
public void LoadIrDriver(string filePath)
```

Loads the IR driver configuration file to the underlying Crestron `IROutputPort`. Logs an error and does nothing if `filePath` is null or empty.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `filePath` | `string` | The full path to the IR configuration file. |
