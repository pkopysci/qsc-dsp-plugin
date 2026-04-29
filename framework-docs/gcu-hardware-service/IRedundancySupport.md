# IRedundancySupport

**Namespace:** `gcu_hardware_service.Redundancy`

Required events, properties, and methods for a device that supports backup/redundant failover to a separate device.

---

## Table of Contents

**Events**
- [RedundancyStateChanged](#redundancystatechanged)
- [BackupDeviceConnectionChanged](#backupdeviceconnectionchanged)

**Properties**
- [PrimaryDeviceActive](#primarydeviceactive)
- [BackupDeviceActive](#backupdeviceactive)
- [BackupDeviceOnline](#backupdeviceonline)
- [BackupDeviceExists](#backupdeviceexists)

**Methods**
- [SetBackupDeviceConnection(string hostname, int port)](#setbackupdeviceconnectionstring-hostname-int-port)

---

## Events

### RedundancyStateChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> RedundancyStateChanged
```

Triggered when the implementation switches between primary and backup/redundant connections. The event argument contains the unique ID of the implementing object.

---

### BackupDeviceConnectionChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> BackupDeviceConnectionChanged
```

Triggered whenever the backup/redundant device loses or establishes a connection. The event argument contains the unique ID of the implementing object.

---

## Properties

### PrimaryDeviceActive

```csharp
bool PrimaryDeviceActive { get; }
```

`true` = the main/primary connection is in use; `false` otherwise.

---

### BackupDeviceActive

```csharp
bool BackupDeviceActive { get; }
```

`true` = the backup/redundant device is in use.

---

### BackupDeviceOnline

```csharp
bool BackupDeviceOnline { get; }
```

`true` = the backup device connection is established; `false` = no connection.

---

### BackupDeviceExists

```csharp
bool BackupDeviceExists { get; }
```

`true` = the backup device has been configured by a successful call to `SetBackupDeviceConnection`.

---

## Methods

### SetBackupDeviceConnection(string hostname, int port)

```csharp
void SetBackupDeviceConnection(string hostname, int port)
```

Assign the backup/redundant device TCP connection information. This method is called by the framework after `Initialize()` and before `Connect()`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `hostname` | `string` | The hostname or IP address used to connect to the redundant device. |
| `port` | `int` | The port number used to connect to the device. |
