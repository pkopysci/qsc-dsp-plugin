# gcu-ui-service

Documentation for the `gcu-ui-service` library. This library provides the presentation layer that sits above the application service in the GCU AV framework. It manages user interface plugin creation, Crestron Fusion server integration, and bridges application service events to the connected UI hardware.

---

## Core

| Type | Description |
|------|-------------|
| [IPresentationService](IPresentationService.md) | Minimum interface contract for any presentation service implementation. |
| [PresentationService](PresentationService.md) | Default implementation; orchestrates UI connections, Fusion integration, and app service event subscriptions. Extendable by plugins. |
| [PresentationServiceFactory](PresentationServiceFactory.md) | Static factory for creating `IPresentationService` instances, loading plugins via `DriverLoader`. |

---

## Interfaces

| Type | Description |
|------|-------------|
| [IUserInterface](IUserInterface.md) | Required contract for all UI plugin implementations. Defines the `SetUiData` / `Initialize` / `Connect` lifecycle. |
| [ICrestronUserInterface](ICrestronUserInterface.md) | Optional interface for plugins that require access to the root `CrestronControlSystem` object. |
| [IErrorInterface](IErrorInterface.md) | Optional interface for plugins that support basic device error reporting from the Presentation Service. |
| [IUiStatusMonitor](IUiStatusMonitor.md) | Optional interface for plugins that require detailed status information about other user interfaces in the configuration. |
| [IUsesApplicationService](IUsesApplicationService.md) | Optional interface for plugins that require a direct reference to the running `IApplicationService`. |

---

## Fusion

| Type | Description |
|------|-------------|
| [IFusionInterface](IFusionInterface.md) | Full contract for a Crestron Fusion server connection, including events, state updates, source tracking, and error management. |
| [FusionInterface](FusionInterface.md) | Default implementation of `IFusionInterface`. |
| [IFusionDeviceUse](IFusionDeviceUse.md) | Interface for tracking AV source and display use time via Fusion. |
| [FusionDeviceUse](FusionDeviceUse.md) | Default implementation of `IFusionDeviceUse`. |
| [IFusionErrorManager](IFusionErrorManager.md) | Interface for queued device offline error reporting to the Fusion server. |

---

## Utility

| Type | Description |
|------|-------------|
| [TransportTypes](TransportTypes.md) | Enumeration of transport control command types used by UI plugins. |
