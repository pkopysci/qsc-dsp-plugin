# ControlSystem

**Namespace:** `gcu_avf`

**Inherits:** `CrestronControlSystem`

Main control system entry point for the GCU AV Framework. Orchestrates the full startup sequence: loading configuration, creating the infrastructure service, application service, and presentation service, then connecting all hardware devices. Also handles graceful shutdown by disposing services when the Crestron program stops.

The startup sequence initiated from `InitializeSystem()` is:

1. `ConfigurationService.LoadConfig()` — parses the room configuration file
2. `InfrastructureServiceFactory.CreateInfrastructureService()` — creates hardware drivers
3. `ApplicationControlFactory.CreateAppService()` — creates the application control layer
4. `PresentationServiceFactory.CreatePresentationService()` — creates the UI layer
5. `IPresentationService.Initialize()` — connects all user interfaces and Fusion
6. `IInfrastructureService.ConnectAllDevices()` — registers and connects all hardware

---

## Table of Contents

**Constructors**
- [ControlSystem()](#controlsystem-1)

**Methods**
- [InitializeSystem()](#initializesystem)

---

## Constructors

### ControlSystem()

```csharp
public ControlSystem()
```

Initializes the control system. Sets the maximum number of user threads to 100, registers the program status event handler for graceful shutdown, and redirects console output to a Crestron-compatible text writer.

---

## Methods

### InitializeSystem()

```csharp
public override void InitializeSystem()
```

Called by the Crestron runtime after the control system is instantiated. Launches an asynchronous task that waits 1 second for Crestron firmware to complete its startup sequence, initializes the logger, then calls the internal `Startup()` method to begin the full framework initialization sequence.
