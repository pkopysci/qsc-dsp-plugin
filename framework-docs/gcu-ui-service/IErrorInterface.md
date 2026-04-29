# IErrorInterface

**Namespace:** `gcu_ui_service.Interfaces`

Required events, methods, and properties for implementing a user interface that supports basic error reporting. Used when the Presentation Service needs to push device-level error notices directly to the UI. For more detailed error information about other user interfaces, implement [`IUiStatusMonitor`](IUiStatusMonitor.md) instead.

---

## Table of Contents

**Methods**
- [AddDeviceError(string id, string message)](#adddeviceerrorstring-id-string-message)
- [ClearDeviceError(string id)](#cleardeviceerrorstring-id)

---

## Methods

### AddDeviceError(string id, string message)

```csharp
void AddDeviceError(string id, string message)
```

Add an external error to the user interface. Used when adding error notices thrown by the Presentation Service that are not originated by the Application Service.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the error to add. |
| `message` | `string` | The error message to display on the UI. |

---

### ClearDeviceError(string id)

```csharp
void ClearDeviceError(string id)
```

Remove an existing error from the UI.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the error to remove. |
