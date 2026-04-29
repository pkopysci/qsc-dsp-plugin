# Authentication

**Namespace:** `gcu_domain_service.Data.ConnectionData`

**Inherits:** [`BaseData`](BaseData.md)

Login information used to connect to a device for TCP/IP control. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [UserName](#username)
- [Password](#password)

---

## Properties

### UserName

```csharp
public string UserName { get; set; }
```

**Type:** `string`

Gets or sets the username used to log into the target device for control. Defaults to `string.Empty`.

---

### Password

```csharp
public string Password { get; set; }
```

**Type:** `string`

Gets or sets the password used to log into the target device for control. Defaults to `string.Empty`.
