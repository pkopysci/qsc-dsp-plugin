# DirectoryHelper

**Namespace:** `gcu_common_utils.FileOps`

Provides static methods for reading files into memory and working with file system paths on Crestron control systems.

---

## Table of Contents

**Methods**
- [NormalizePath(string currentPath)](#normalizepathstring-currentpath)
- [GetUserFolder()](#getuserfolder)
- [FileExists(string filepath)](#fileexistsstring-filepath)

---

## Methods

### NormalizePath(string currentPath)

```csharp
public static string NormalizePath(string currentPath)
```

Converts the directory delimiters depending on the underlying platform type. If the platform is a server then all `\` are replaced with `/`, otherwise all `/` are replaced with `\`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `currentPath` | `string` | The relative or absolute file path to correct. |

**Returns:** The same file path given as an argument but formatted correctly based on the platform.

**Exceptions**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | If `currentPath` is null or empty. |

---

### GetUserFolder()

```csharp
public static string GetUserFolder()
```

Returns the directory path to the control system's User folder. The returned string does not include the trailing `/`.

**Returns:** The application directory with `/User` or `/user` appended, depending on the platform.

---

### FileExists(string filepath)

```csharp
public static bool FileExists(string filepath)
```

Check to see if a given file exists on the control system.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `filepath` | `string` | The full filepath to check, including file extension. |

**Returns:** `true` if the file exists; `false` otherwise.
