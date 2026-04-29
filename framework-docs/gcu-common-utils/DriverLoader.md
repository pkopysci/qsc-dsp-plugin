# DriverLoader

**Namespace:** `gcu_common_utils.FileOps`

Static helper class used to load a Crestron Certified Driver (CCD) from a DLL using reflection. Drivers are loaded from the `net8-plugins` directory within the control system's User folder.

---

## Table of Contents

**Methods**
- [LoadDriverInstance\<T\>(string assemblyName, string interfaceName, string transportName)](#loaddriverinstancetstring-assemblyname-string-interfacename-string-transportname)
- [LoadClassByInterface\<T\>(string assemblyName, string className, string interfaceName)](#loadclassbyinterfacetstring-assemblyname-string-classname-string-interfacename)
- [LoadClassByInterface\<T\>(string assemblyName, string className, string interfaceName, object[] constructorArgs)](#loadclassbyinterfacetstring-assemblyname-string-classname-string-interfacename-object-constructorargs)
- [GetTransportType(string connectionTag)](#gettransporttypestring-connectiontag)

---

## Methods

### LoadDriverInstance\<T\>(string assemblyName, string interfaceName, string transportName)

```csharp
public static T? LoadDriverInstance<T>(string assemblyName, string interfaceName, string transportName)
```

Return an instance of a Crestron Certified Driver by searching the target assembly for a type that implements both the specified CCD interface and transport interface.

**Type Parameters**

| Name | Description |
|------|-------------|
| `T` | The expected return type. |

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `assemblyName` | `string` | The full directory and file name with extension of the target assembly. |
| `interfaceName` | `string` | The CCD interface to search for in the driver DLL. |
| `transportName` | `string` | The CCD transport type to search for in the driver DLL. |

**Returns:** The target `T` object if found in the assembly, or the default value of `T` if no matching driver is found.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If any argument is null or empty. |
| `Exception` | Will propagate exceptions from `System.Reflection`. |

---

### LoadClassByInterface\<T\>(string assemblyName, string className, string interfaceName)

```csharp
public static T? LoadClassByInterface<T>(string assemblyName, string className, string interfaceName)
```

Return an instance of a class based on the defined interface name. This overload uses the default (parameterless) constructor.

**Type Parameters**

| Name | Description |
|------|-------------|
| `T` | The expected return type. |

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `assemblyName` | `string` | The full directory and file name with extension of the target assembly. |
| `className` | `string` | The class to search for in the assembly DLL. |
| `interfaceName` | `string` | The interface to search for in the assembly DLL. |

**Returns:** The target `T` object if found in the assembly, or the default value of `T` if no matching class is found.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `assemblyName` or `interfaceName` is null or empty. |

---

### LoadClassByInterface\<T\>(string assemblyName, string className, string interfaceName, object[] constructorArgs)

```csharp
public static T? LoadClassByInterface<T>(string assemblyName, string className, string interfaceName, object[] constructorArgs)
```

Return an instance of a class based on the defined interface name. This version of the method allows for constructor arguments.

**Type Parameters**

| Name | Description |
|------|-------------|
| `T` | The expected return type. |

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `assemblyName` | `string` | The full directory and file name with extension of the target assembly. |
| `className` | `string` | The class to search for in the assembly DLL. |
| `interfaceName` | `string` | The interface to search for in the assembly DLL. |
| `constructorArgs` | `object[]` | The constructor arguments to pass when creating the object. |

**Returns:** The target `T` object if found in the assembly, or the default value of `T` if no matching class is found.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `assemblyName` or `interfaceName` is null or empty. |

---

### GetTransportType(string connectionTag)

```csharp
public static string GetTransportType(string connectionTag)
```

Resolves a configuration service tag to a Crestron Certified Driver transport interface type name. Writes an error to the logging system if the lookup fails.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `connectionTag` | `string` | The tag to evaluate. Cannot be null or empty. Supported values (case-insensitive): `TCP`, `HTTP`, `TELNET`, `REST`, `SERIAL`, `IR`. |

**Returns:** The name of the CCD transport type, or the empty string if the given tag is not supported.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `connectionTag` is null or empty. |
