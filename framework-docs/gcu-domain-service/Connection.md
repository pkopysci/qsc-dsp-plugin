# Connection

**Namespace:** `gcu_domain_service.Data.ConnectionData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data object for TCP/IP, RS-232, or IR control of a device. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Transport](#transport)
- [Driver](#driver)
- [Host](#host)
- [BackupHost](#backuphost)
- [MacAddress](#macaddress)
- [Port](#port)
- [BackupPort](#backupport)
- [Authentication](#authentication)
- [ComSpec](#comspec)

---

## Properties

### Transport

```csharp
public string Transport { get; set; }
```

**Type:** `string`

Gets or sets the communication method. Supported values: `tcp`, `ir`, `serial`. Defaults to `string.Empty`.

---

### Driver

```csharp
public string Driver { get; set; }
```

**Type:** `string`

Gets or sets the DLL file name that should be loaded when using Crestron Certified Drivers. Defaults to `string.Empty`.

---

### Host

```csharp
public string Host { get; set; }
```

**Type:** `string`

Gets or sets the IP address or hostname used to control the device. If `Transport` is `serial` or `IR`, this should contain either `control` for the root control system, or the ID of the endpoint the device is connected to. Defaults to `string.Empty`.

---

### BackupHost

```csharp
public string BackupHost { get; set; }
```

**Type:** `string`

The hostname or IP address of a redundant/backup device. Can be left at the default value (`string.Empty`) if there is no redundancy in the system.

---

### MacAddress

```csharp
public string MacAddress { get; set; }
```

**Type:** `string`

MAC address of the device, used for Wake-on-LAN. Format: `"00.00.00.00.00.00"`. Defaults to `string.Empty`.

---

### Port

```csharp
public int Port { get; set; }
```

**Type:** `int`

Gets or sets the TCP/IP, RS-232, or IR port number used to control the device. Defaults to `0`.

---

### BackupPort

```csharp
public int BackupPort { get; set; }
```

**Type:** `int`

The port used to connect to a redundant/backup device. Can be omitted (`0`) if there is no backup in the system design.

---

### Authentication

```csharp
public Authentication Authentication { get; set; }
```

**Type:** [`Authentication`](Authentication.md)

Gets or sets the credentials used to log into the device. Defaults to an empty `Authentication` instance.

---

### ComSpec

```csharp
public ComSpec ComSpec { get; set; }
```

**Type:** [`ComSpec`](ComSpec.md)

Gets or sets the serial communication protocol specification if the device is serial controlled. Defaults to an empty `ComSpec` instance.
