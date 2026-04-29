# DomainFactory

**Namespace:** `gcu_domain_service`

Static helper class for building an [`IDomainService`](IDomainService.md) hardware provider service from JSON configuration data.

---

## Table of Contents

**Methods**
- [CreateDomainFromJson(string data)](#createdomainfromjsonstring-data)

---

## Methods

### CreateDomainFromJson(string data)

```csharp
public static IDomainService CreateDomainFromJson(string data)
```

Attempt to create an `IDomainService` object from the given serialized JSON configuration data. If deserialization fails or an exception is thrown, the error is written to the logging system and an empty `DomainService` is returned.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `data` | `string` | The serialized JSON string to parse into a `DataContainer`. |

**Returns:** A new `IDomainService` backed by the parsed configuration data, or an empty `DomainService` if parsing fails.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `data` is null or empty. |
