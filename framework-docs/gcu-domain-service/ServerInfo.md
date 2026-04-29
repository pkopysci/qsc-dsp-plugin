# ServerInfo

**Namespace:** `gcu_domain_service.Data`

Connection information for a remote dependency server (e.g., an SFTP server used for driver or configuration file delivery).

---

## Table of Contents

**Properties**
- [Host](#host)
- [User](#user)
- [Key](#key)

---

## Properties

### Host

```csharp
public string Host { get; set; }
```

**Type:** `string`

The IP address or hostname of the remote dependency server. Defaults to `string.Empty`.

---

### User

```csharp
public string User { get; set; }
```

**Type:** `string`

The username used to authenticate with the remote server. Defaults to `string.Empty`.

---

### Key

```csharp
public string Key { get; set; }
```

**Type:** `string`

The SSH private key or password used to authenticate with the remote server. Defaults to `string.Empty`.
