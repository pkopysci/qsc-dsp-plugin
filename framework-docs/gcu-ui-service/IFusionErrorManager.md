# IFusionErrorManager

**Namespace:** `gcu_ui_service.Fusion.ErrorManagement`

Common methods and properties used for reporting errors to a Crestron Fusion server. Supports queued error management, automatically progressing through queued messages and sending an "OK" status when the queue is empty.

---

## Table of Contents

**Methods**
- [AddOfflineDevice(string devId, string message)](#addofflinedevicestring-devid-string-message)
- [ClearOfflineDevice(string devId)](#clearofflinedevicestring-devid)

---

## Methods

### AddOfflineDevice(string devId, string message)

```csharp
void AddOfflineDevice(string devId, string message)
```

Add an offline error to the current error queue for the target device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `devId` | `string` | The unique ID of the device to report an error on. Used to locate the device when clearing an error. |
| `message` | `string` | The error message text to send to the Fusion server. |

---

### ClearOfflineDevice(string devId)

```csharp
void ClearOfflineDevice(string devId)
```

Remove an error from the current queue. If it is the currently displayed error, the next error in the queue will be sent to the server. If there are no more errors in the queue, an "OK" message will be sent to the server.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `devId` | `string` | The unique ID of the device that was assigned when calling `AddOfflineDevice()`. |
