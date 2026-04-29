# CustomEventAppService

**Namespace:** `gcu_application_service.CustomEvents`

**Inherits:** [`ApplicationService`](ApplicationService.md)

**Implements:** [`ICustomEventAppService`](ICustomEventAppService.md)

An abstract extension of `ApplicationService` that adds support for custom event behaviors (e.g., entering a "theater mode" or other non-standard system states). Plugin authors can extend this class instead of `ApplicationService` to expose custom event controls to the presentation layer.

---

## Table of Contents

**Events**
- [CustomEventStateChanged](#customeventstatechanged)

**Protected Fields**
- [customEvents](#customevents)
- [events](#events)

**Methods**
- [QueryAllCustomEvents()](#queryallcustomevents)
- [ChangeCustomEventState(string tag, bool state)](#changecustomeventstatestring-tag-bool-state)
- [QueryCustomEventState(string tag)](#querycustomeventstatestring-tag)
- [NotifyStateChange(string tag)](#notifystatechangestring-tag)

---

## Events

### CustomEventStateChanged

```csharp
public event EventHandler<GenericSingleEventArgs<string>>? CustomEventStateChanged
```

Triggered when a supported custom behavior changes state. The event arg is the tag of the event that changed.

---

## Protected Fields

### customEvents

```csharp
protected Dictionary<string, Action<bool>> customEvents
```

A collection mapping custom event tags to the action that will be invoked when the event is triggered.

---

### events

```csharp
protected Dictionary<string, CustomEventInfoContainer> events
```

A collection of event data objects associated with all supported event tags.

---

## Methods

### QueryAllCustomEvents()

```csharp
public virtual ReadOnlyCollection<CustomEventInfoContainer> QueryAllCustomEvents()
```

Query the application service instance for all supported custom event data objects.

**Returns:** A collection of `CustomEventInfoContainer` objects for all supported events.

---

### ChangeCustomEventState(string tag, bool state)

```csharp
public virtual void ChangeCustomEventState(string tag, bool state)
```

Trigger the action associated with the given tag. Logs an error if no matching tag is found.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `tag` | `string` | The unique tag of the custom event to trigger. |
| `state` | `bool` | The state to pass to the associated action. |

---

### QueryCustomEventState(string tag)

```csharp
public virtual bool QueryCustomEventState(string tag)
```

Query the current state of a target custom event. Logs an error if no matching tag is found.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `tag` | `string` | The unique tag of the event to query. |

**Returns:** `true` if the event is active; `false` otherwise.

---

### NotifyStateChange(string tag)

```csharp
protected virtual void NotifyStateChange(string tag)
```

Triggers the `CustomEventStateChanged` event with the given tag. Called by subclasses when a custom state change occurs.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `tag` | `string` | The tag that will be sent to subscribers with the state change notice. |
