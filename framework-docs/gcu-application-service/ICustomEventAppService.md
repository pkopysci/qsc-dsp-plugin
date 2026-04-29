# ICustomEventAppService

**Namespace:** `gcu_application_service.CustomEvents`

Common properties and methods associated with a custom event application service. Used to add non-standard system states (e.g., "theater mode") to an application service plugin.

---

## Table of Contents

**Events**
- [CustomEventStateChanged](#customeventstatechanged)

**Methods**
- [QueryAllCustomEvents()](#queryallcustomevents)
- [ChangeCustomEventState(string tag, bool state)](#changecustomeventstatestring-tag-bool-state)
- [QueryCustomEventState(string tag)](#querycustomeventstatestring-tag)

---

## Events

### CustomEventStateChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>> CustomEventStateChanged
```

Triggered when a supported custom behavior changes state. The event arg is the tag of the event that changed.

---

## Methods

### QueryAllCustomEvents()

```csharp
ReadOnlyCollection<CustomEventInfoContainer> QueryAllCustomEvents()
```

Query the application service instance for all supported custom event tags.

**Returns:** A collection of data objects representing all custom events supported by the application service.

---

### ChangeCustomEventState(string tag, bool state)

```csharp
void ChangeCustomEventState(string tag, bool state)
```

Trigger a non-standard behavior sequence. Logs a warning if no behavior with the supplied tag is found.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `tag` | `string` | The unique tag for the behavior to trigger. |
| `state` | `bool` | The new state to apply to the custom event. |

---

### QueryCustomEventState(string tag)

```csharp
bool QueryCustomEventState(string tag)
```

Query the current state of a target custom behavior.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `tag` | `string` | The unique tag of the behavior to query. |

**Returns:** `true` if the behavior is active; `false` otherwise.
