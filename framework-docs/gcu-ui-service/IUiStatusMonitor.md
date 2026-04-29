# IUiStatusMonitor

**Namespace:** `gcu_ui_service.Interfaces`

Events, methods, and properties for any user interface that requires more detailed status information about other user interfaces in the configuration. This is an optional interface for designs that require more data than what is provided by [`IErrorInterface`](IErrorInterface.md).

---

## Table of Contents

**Methods**
- [UpdateUserInterfaceStatus(UserInterfaceDataContainer userInterfaceData)](#updateuserinterfacestatususerinterfacedatacontainer-userinterfacedata)

---

## Methods

### UpdateUserInterfaceStatus(UserInterfaceDataContainer userInterfaceData)

```csharp
void UpdateUserInterfaceStatus(UserInterfaceDataContainer userInterfaceData)
```

Update the UI with the current status of another interface implementation.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `userInterfaceData` | `UserInterfaceDataContainer` | The updated UI data object including online/offline status and configuration details. |
