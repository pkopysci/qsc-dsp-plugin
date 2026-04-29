# IApplicationService

**Namespace:** `gcu_application_service`

**Implements:** [`ISystemPowerApp`](ISystemPowerApp.md), [`IDisplayControlApp`](IDisplayControlApp.md), [`IEndpointControlApp`](IEndpointControlApp.md), [`IAudioControlApp`](IAudioControlApp.md), [`IAvRoutingApp`](IAvRoutingApp.md), [`ITransportControlApp`](ITransportControlApp.md), [`ILightingControlApp`](ILightingControlApp.md)

The top-level application service interface. Aggregates all subsystem control interfaces and adds global video control, room information, and user interface queries.

---

## Table of Contents

**Events**
- [GlobalVideoFreezeChanged](#globalvideofreezechanged)
- [GlobalVideoBlankChanged](#globalvideoblankchanged)

**Methods**
- [GetAllUserInterfaces()](#getalluserinterfaces)
- [UpdateUserInterfaceConnectionStatus(string id, bool connectionStatus)](#updateuserinterfaceconnectionstatusstring-id-bool-connectionstatus)
- [GetFusionInterface()](#getfusioninterface)
- [GetRoomInfo()](#getroominfo)
- [SetGlobalVideoBlank(bool state)](#setglobalvideoblankbool-state)
- [SetGlobalVideoFreeze(bool state)](#setglobalvideofreezebbool-state)
- [QueryGlobalVideoBlank()](#queryglobalvideoblank)
- [QueryGlobalVideoFreeze()](#queryglobalvideofreeze)
- [Initialize(IInfrastructureService hwService, IDomainService domain)](#initializeiinfrastructureservice-hwservice-idomainservice-domain)

---

## Events

### GlobalVideoFreezeChanged

```csharp
event EventHandler GlobalVideoFreezeChanged
```

Triggered when the global freeze state has changed.

---

### GlobalVideoBlankChanged

```csharp
event EventHandler GlobalVideoBlankChanged
```

Triggered when the global video blank status has changed.

---

## Methods

### GetAllUserInterfaces()

```csharp
ReadOnlyCollection<UserInterfaceDataContainer> GetAllUserInterfaces()
```

Query the service for information on all user interfaces included in the system configuration.

**Returns:** A data collection representing all user interfaces that will connect with this system.

---

### UpdateUserInterfaceConnectionStatus(string id, bool connectionStatus)

```csharp
void UpdateUserInterfaceConnectionStatus(string id, bool connectionStatus)
```

Update the interface data collection with the current connection state of the UI. This is typically called by the presentation service.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the UI being updated. |
| `connectionStatus` | `bool` | `true` = is online; `false` = offline. |

---

### GetFusionInterface()

```csharp
UserInterfaceDataContainer GetFusionInterface()
```

Query the service for Fusion connection information. The returned container includes:
- `Id` — GUID used for Fusion discovery
- `Label` — the room name to display in Fusion
- `IpId` — the IP-ID used to establish a connection with the Fusion server

**Returns:** The Fusion information as defined in the system configuration.

---

### GetRoomInfo()

```csharp
RoomInfoContainer GetRoomInfo()
```

Query the application service for the room information set in the configuration file.

**Returns:** A data object containing general room information.

---

### SetGlobalVideoBlank(bool state)

```csharp
void SetGlobalVideoBlank(bool state)
```

Blank the output on all video endpoints. Applied either at the displays or on the AV routing hardware depending on configuration.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `state` | `bool` | `true` = set blank active (no video); `false` = set blank off (show video). |

---

### SetGlobalVideoFreeze(bool state)

```csharp
void SetGlobalVideoFreeze(bool state)
```

Freeze the video output on all display endpoints. Applied either at the displays or on the AV routing hardware.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `state` | `bool` | `true` = freeze on (no motion); `false` = freeze off (normal video). |

---

### QueryGlobalVideoBlank()

```csharp
bool QueryGlobalVideoBlank()
```

Get the current state of the global video blank.

**Returns:** `true` if video output is blanked; `false` if video output is showing.

---

### QueryGlobalVideoFreeze()

```csharp
bool QueryGlobalVideoFreeze()
```

Get the current state of the global video freeze.

**Returns:** `true` if global video is frozen; `false` if normal video output.

---

### Initialize(IInfrastructureService hwService, IDomainService domain)

```csharp
void Initialize(IInfrastructureService hwService, IDomainService domain)
```

Creates internal hooks and instantiates all application logic control objects.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `hwService` | `IInfrastructureService` | The hardware control service used to send commands to devices. |
| `domain` | `IDomainService` | The configuration domain for this system. |
