# Logger

**Namespace:** `gcu_common_utils.Logging`

Static logging system for writing messages to the Crestron control system error log, console output, and a rolling JSON log file. Built on [Serilog](https://serilog.net/).

All log statements include a service type, device type, ID tag, and message. The log level can be toggled at runtime via the `setdebug ON | OFF` console command or programmatically via `EnableDebug()` / `DisableDebug()`.

---

## Table of Contents

**Properties**
- [IsInitialized](#isinitialized)

**Methods**
- [Initialize(CrestronControlSystem controlSystem, string programId)](#initializecrestroncontrolsystem-controlsystem-string-programid)
- [EnableDebug()](#enabledebug)
- [DisableDebug()](#disabledebug)
- [Destroy()](#destroy)
- [Error(LogServiceTypes service, LogDeviceTypes device, string id, string message)](#errorlogservicetypes-service-logdevicetypes-device-string-id-string-message)
- [Error(LogServiceTypes service, LogDeviceTypes device, string id, Exception exception)](#errorlogservicetypes-service-logdevicetypes-device-string-id-exception-exception)
- [Warn(LogServiceTypes service, LogDeviceTypes device, string id, string message)](#warnlogservicetypes-service-logdevicetypes-device-string-id-string-message)
- [Notice(LogServiceTypes service, LogDeviceTypes device, string id, string message)](#noticelogservicetypes-service-logdevicetypes-device-string-id-string-message)
- [Debug(LogServiceTypes service, LogDeviceTypes device, string id, string message)](#debuglogservicetypes-service-logdevicetypes-device-string-id-string-message)

---

## Properties

### IsInitialized

```csharp
public static bool IsInitialized { get; }
```

`true` = logging has been enabled and configured; `false` = `Initialize()` has not been called or it failed.

---

## Methods

### Initialize(CrestronControlSystem controlSystem, string programId)

```csharp
public static void Initialize(CrestronControlSystem controlSystem, string programId = "")
```

Trigger internal logging system setup and assign the program ID information. If logging has already been initialized, the previous configuration is closed and flushed before re-initializing. Registers the `setdebug ON | OFF` console command.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `controlSystem` | `CrestronControlSystem` | The entry point into the control system program. |
| `programId` | `string` | A unique program identifier included in each log statement. Defaults to the control system's program number (zero-padded to 2 digits) if not provided. |

**Remarks:** In a `DEBUG` build, the logging level is automatically set to `Debug`. In a release build, the minimum level defaults to `Information`.

---

### EnableDebug()

```csharp
public static void EnableDebug()
```

Lowers the logging level to `Debug`, enabling more verbose logging to all sinks.

---

### DisableDebug()

```csharp
public static void DisableDebug()
```

Sets the logging level back to a minimum of `Warning` for all sinks.

---

### Destroy()

```csharp
public static void Destroy()
```

Closes and flushes all internal logging mechanisms. Also sets `IsInitialized` to `false`. Does nothing if logging has not been initialized.

---

### Error(LogServiceTypes service, LogDeviceTypes device, string id, string message)

```csharp
public static void Error(LogServiceTypes service, LogDeviceTypes device, string id, string message)
```

Write a simple error message to all sinks in the logging system.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `service` | `LogServiceTypes` | The service category associated with the log entry. |
| `device` | `LogDeviceTypes` | The device type associated with the log entry. |
| `id` | `string` | A unique identifier for the calling object or component. |
| `message` | `string` | The error message to write. |

---

### Error(LogServiceTypes service, LogDeviceTypes device, string id, Exception exception)

```csharp
public static void Error(LogServiceTypes service, LogDeviceTypes device, string id, Exception exception)
```

Writes an exception to all sinks. This writes both the exception message and stack trace.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `service` | `LogServiceTypes` | The service category associated with the log entry. |
| `device` | `LogDeviceTypes` | The device type associated with the log entry. |
| `id` | `string` | A unique identifier for the calling object or component. |
| `exception` | `Exception` | The .NET exception to log. |

---

### Warn(LogServiceTypes service, LogDeviceTypes device, string id, string message)

```csharp
public static void Warn(LogServiceTypes service, LogDeviceTypes device, string id, string message)
```

Writes a simple warning message to all logging sinks.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `service` | `LogServiceTypes` | The service category associated with the log entry. |
| `device` | `LogDeviceTypes` | The device type associated with the log entry. |
| `id` | `string` | A unique identifier for the calling object or component. |
| `message` | `string` | The warning message to write. |

---

### Notice(LogServiceTypes service, LogDeviceTypes device, string id, string message)

```csharp
public static void Notice(LogServiceTypes service, LogDeviceTypes device, string id, string message)
```

Writes a simple notice (informational) message to all logging sinks.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `service` | `LogServiceTypes` | The service category associated with the log entry. |
| `device` | `LogDeviceTypes` | The device type associated with the log entry. |
| `id` | `string` | A unique identifier for the calling object or component. |
| `message` | `string` | The notice message to write. |

---

### Debug(LogServiceTypes service, LogDeviceTypes device, string id, string message)

```csharp
public static void Debug(LogServiceTypes service, LogDeviceTypes device, string id, string message)
```

Writes a simple debug message to all logging sinks. This will only be written if debug logging is enabled either from the CLI (`setdebug ON`) or by calling `EnableDebug()`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `service` | `LogServiceTypes` | The service category associated with the log entry. |
| `device` | `LogDeviceTypes` | The device type associated with the log entry. |
| `id` | `string` | A unique identifier for the calling object or component. |
| `message` | `string` | The debug message to write. |

---

## Related Types

- [`LogServiceTypes`](LogServiceTypes.md) — Enum of supported service categories.
- [`LogDeviceTypes`](LogDeviceTypes.md) — Enum of supported device types.
