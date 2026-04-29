# ParameterValidator

**Namespace:** `gcu_common_utils.Validation`

Static helper class for checking method arguments. Use this class at method entry points to enforce non-null and non-empty preconditions on parameters.

---

## Table of Contents

**Methods**
- [ThrowIfNull(object? param, string methodName, string paramName)](#throwifnullobject-param-string-methodname-string-paramname)
- [ThrowIfNullOrEmpty(string param, string methodName, string paramName)](#throwifnulloremptystring-param-string-methodname-string-paramname)

---

## Methods

### ThrowIfNull(object? param, string methodName, string paramName)

```csharp
public static void ThrowIfNull(object? param, string methodName, string paramName)
```

Throws an exception if the parameter is null.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `param` | `object?` | The object being evaluated for null. |
| `methodName` | `string` | The name of the method running the check, used in the exception message. |
| `paramName` | `string` | The name of the parameter being checked, used in the exception message. |

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | If `param` is null. The message format is: `{methodName}() - {paramName} cannot be null.` |

---

### ThrowIfNullOrEmpty(string param, string methodName, string paramName)

```csharp
public static void ThrowIfNullOrEmpty(string param, string methodName, string paramName)
```

Throws an exception if the string parameter is null or empty.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `param` | `string` | The string to evaluate. |
| `methodName` | `string` | The name of the method running the check, used in the exception message. |
| `paramName` | `string` | The name of the parameter being checked, used in the exception message. |

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `param` is null or empty. The message format is: `{methodName}() - {paramName} cannot be null or empty.` |
