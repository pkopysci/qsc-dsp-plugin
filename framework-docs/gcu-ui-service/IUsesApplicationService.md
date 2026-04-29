# IUsesApplicationService

**Namespace:** `gcu_ui_service.Interfaces`

Interface for any UI implementation that requires a direct connection with the application service, such as a REST API server. Implement this interface alongside [`IUserInterface`](IUserInterface.md) when the plugin needs to send commands or query state directly from the `IApplicationService`.

---

## Table of Contents

**Methods**
- [SetApplicationService(IApplicationService applicationService)](#setapplicationserviceiapplicationservice-applicationservice)

---

## Methods

### SetApplicationService(IApplicationService applicationService)

```csharp
void SetApplicationService(IApplicationService applicationService)
```

Sets internal references for sending state commands and queries to the system application service.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `applicationService` | `IApplicationService` | The running application service for the system. |

**Exceptions**

| Type | Condition |
|------|-----------|
| `ArgumentNullException` | If `applicationService` is null. |
