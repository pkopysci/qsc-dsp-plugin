# Logging Types

**Namespace:** `gcu_common_utils.Logging.LoggingTypes`

Enumerations used to categorize log entries written through the [`Logger`](Logger.md) class. Each log call requires both a `LogServiceTypes` and a `LogDeviceTypes` value to help filter and identify log output.

---

## Table of Contents

- [LogServiceTypes](#logservicetypes)
- [LogDeviceTypes](#logdevicetypes)

---

## LogServiceTypes

```csharp
public enum LogServiceTypes
```

Defines the service layer or application area that is producing the log entry.

| Value | Description |
|-------|-------------|
| `ControlSystem` | Log entries originating from the control system program entry point. |
| `Common` | Log entries from shared utility or common library code. |
| `Configuration` | Log entries related to configuration loading or parsing. |
| `Domain` | Log entries from domain/business logic layer. |
| `Hardware` | Log entries from hardware driver or hardware abstraction code. |
| `Application` | Log entries from the application layer. |
| `Presentation` | Log entries from the UI or presentation layer. |
| `UiPlugin` | Log entries from a UI plugin. |

---

## LogDeviceTypes

```csharp
public enum LogDeviceTypes
```

Defines the type of device associated with a log entry.

| Value | Description |
|-------|-------------|
| `NotApplicable` | No specific device type applies to this log entry. |
| `Display` | Log entry is associated with a display device. |
| `Avr` | Log entry is associated with an audio/video receiver. |
| `Dsp` | Log entry is associated with a digital signal processor. |
| `Ctv` | Log entry is associated with a cable/satellite TV device. |
| `Bluray` | Log entry is associated with a Blu-ray player. |
| `VideoWall` | Log entry is associated with a video wall controller. |
| `Lighting` | Log entry is associated with a lighting control system. |
| `Camera` | Log entry is associated with a camera device. |
| `Endpoint` | Log entry is associated with a generic endpoint device. |
| `UserInterface` | Log entry is associated with a user interface device or panel. |
