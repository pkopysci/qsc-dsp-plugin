# FusionDeviceUse

**Namespace:** `gcu_ui_service.Fusion.DeviceUse`

**Implements:** [`IFusionDeviceUse`](IFusionDeviceUse.md)

Device use message object for sending data to the Fusion server. Manages separate tracking collections for AV source devices and displays. When a use session is stopped, the elapsed time is calculated in minutes and sent to the Fusion server as a formatted usage string.

---

## Table of Contents

**Constructors**
- [FusionDeviceUse(FusionRoom fusion)](#fusiondeviceusefusionroom-fusion)

**Methods**
- [AddDeviceToUseTracking(string id, string label)](#adddevicetousetrackinstring-id-string-label)
- [StartDeviceUse(string id)](#startdeviceusestring-id)
- [StopDeviceUse(string id)](#stopdeviceusestring-id)
- [AddDisplayToUseTracking(string id, string label)](#adddisplaytousetrackinstring-id-string-label)
- [StartDisplayUse(string id)](#startdisplayusestring-id)
- [StopDisplayUse(string id)](#stopdisplayusestring-id)

---

## Constructors

### FusionDeviceUse(FusionRoom fusion)

```csharp
public FusionDeviceUse(FusionRoom fusion)
```

Instantiates a new instance of `FusionDeviceUse`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `fusion` | `FusionRoom` | The Crestron Fusion connection object used for server communication. |

---

## Methods

### AddDeviceToUseTracking(string id, string label)

```csharp
public void AddDeviceToUseTracking(string id, string label)
```

Add a device to the internal collection used for tracking use. See [`IFusionDeviceUse.AddDeviceToUseTracking`](IFusionDeviceUse.md#adddevicetousetrackinstring-id-string-label).

---

### StartDeviceUse(string id)

```csharp
public void StartDeviceUse(string id)
```

Start recording use time for the target device. See [`IFusionDeviceUse.StartDeviceUse`](IFusionDeviceUse.md#startdeviceusestring-id).

---

### StopDeviceUse(string id)

```csharp
public void StopDeviceUse(string id)
```

Stop recording the use time for the target device and send a usage log to the Fusion server. See [`IFusionDeviceUse.StopDeviceUse`](IFusionDeviceUse.md#stopdeviceusestring-id).

---

### AddDisplayToUseTracking(string id, string label)

```csharp
public void AddDisplayToUseTracking(string id, string label)
```

Add a display to the internal collection used for tracking use statistics. See [`IFusionDeviceUse.AddDisplayToUseTracking`](IFusionDeviceUse.md#adddisplaytousetrackinstring-id-string-label).

---

### StartDisplayUse(string id)

```csharp
public void StartDisplayUse(string id)
```

Start recording the use time for the target display. See [`IFusionDeviceUse.StartDisplayUse`](IFusionDeviceUse.md#startdisplayusestring-id).

---

### StopDisplayUse(string id)

```csharp
public void StopDisplayUse(string id)
```

Stop recording the use time for the target display and send the data to the Fusion server. See [`IFusionDeviceUse.StopDisplayUse`](IFusionDeviceUse.md#stopdisplayusestring-id).
