# PresentationServiceFactory

**Namespace:** `gcu_ui_service`

Helper class for creating presentation service objects. Provides a public factory method for instantiating `IPresentationService` implementations. Attempts to load a plugin class from the library and class name defined in the room configuration; if no plugin is configured or the load fails, falls back to the default `PresentationService` implementation.

---

## Table of Contents

**Methods**
- [CreatePresentationService(IApplicationService appService, CrestronControlSystem control)](#createpresentationserviceiapplicationservice-appservice-crestroncontrolsystem-control)

---

## Methods

### CreatePresentationService(IApplicationService appService, CrestronControlSystem control)

```csharp
public static IPresentationService CreatePresentationService(IApplicationService appService, CrestronControlSystem control)
```

Creates a full presentation service object that hooks into the application service events. If the room configuration specifies a `PresentationServiceClass` and `PresentationServiceLibrary`, the factory attempts to load the plugin class via `DriverLoader`. If no plugin is configured or the load fails, a default `PresentationService` is returned.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `appService` | `IApplicationService` | The base application service used to control business logic. |
| `control` | `CrestronControlSystem` | The control system entry point for this program. |

**Returns**

An `IPresentationService` instance that can be initialized for interacting with user interface hardware.
