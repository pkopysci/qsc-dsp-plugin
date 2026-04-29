# InfrastructureServiceFactory

**Namespace:** `gcu_hardware_service`

Helper class for creating the `IInfrastructureService` object that will control the actual hardware in the system.

---

## Table of Contents

**Methods**
- [CreateInfrastructureService(IDomainService domain, CrestronControlSystem control)](#createinfrastructureserviceIDomainService-domain-CrestronControlSystem-control)

---

## Methods

### CreateInfrastructureService(IDomainService domain, CrestronControlSystem control)

```csharp
public static IInfrastructureService CreateInfrastructureService(IDomainService domain, CrestronControlSystem control)
```

Create the infrastructure service object that contains all device connections that were defined in the domain. This method will not establish connections to the devices.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `domain` | `IDomainService` | The data class with all device information that was included in the configuration. |
| `control` | `CrestronControlSystem` | The host processor that this program is running on. |

**Returns:** The hardware control service that will be used to send commands to physical devices.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | If `domain` or `control` are null. |
