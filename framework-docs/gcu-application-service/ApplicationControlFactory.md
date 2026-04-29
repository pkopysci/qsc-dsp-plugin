# ApplicationControlFactory

**Namespace:** `gcu_application_service`

Helper class for creating an implementation of `IApplicationService`. Loads either a custom plugin or the default service depending on the system configuration.

---

## Table of Contents

**Methods**
- [CreateAppService(IInfrastructureService hwService, IDomainService domain)](#createappserviceiinfrastructureservice-hwservice-idomainservice-domain)

---

## Methods

### CreateAppService(IInfrastructureService hwService, IDomainService domain)

```csharp
public static IApplicationService? CreateAppService(
    IInfrastructureService hwService,
    IDomainService domain)
```

Creates the application service implementation and stores the hardware connections and control data. If no custom application service is defined in the domain configuration, the default `ApplicationService` is created. If a plugin class and library are specified, it is loaded via reflection using `DriverLoader`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `hwService` | `IInfrastructureService` | The infrastructure implementation for controlling devices. |
| `domain` | `IDomainService` | The system configuration data provider. |

**Returns:** A populated application service that is ready for use, or `null` if creation fails.

**Exceptions**

| Type | Condition |
|------|-----------|
| `ArgumentNullException` | Thrown if `hwService` or `domain` is null. |
