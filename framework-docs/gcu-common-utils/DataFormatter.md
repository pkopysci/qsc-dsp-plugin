# DataFormatter

**Namespace:** `gcu_common_utils.Validation`

Static helper class to standardize and check configuration arguments. Provides utility methods for normalizing string values before comparison or storage.

---

## Table of Contents

**Methods**
- [NormalizeDeviceModel(string arg)](#normalizedevicemodelstring-arg)

---

## Methods

### NormalizeDeviceModel(string arg)

```csharp
public static string NormalizeDeviceModel(string arg)
```

Removes all leading and trailing white spaces and removes any hyphens. Converts the result to uppercase using invariant culture rules.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `arg` | `string` | The config argument to convert. |

**Returns:** A new string with all leading/trailing whitespace and hyphens removed, converted to uppercase. Returns `string.Empty` if `arg` is null or empty.
