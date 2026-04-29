# IDspLogicTriggerSupport

**Namespace:** `gcu_hardware_service.AudioDevices`

Required members for any DSP or other audio device that supports external control of internal logic trigger events. This is an optional interface that DSP plugins may implement alongside [`IDsp`](IDsp.md).

---

## Table of Contents

**Events**
- [DspLogicTriggerStateChanged](#dsplogictriggerstatechanged)

**Methods**
- [AddDspLogicTrigger(string id, string tagName, List\<string\> tags)](#adddsplogictriggerstring-id-string-tagname-liststring-tags)
- [PulseDspLogicTrigger(string id)](#pulsedsplogictriggerstring-id)

---

## Events

### DspLogicTriggerStateChanged

```csharp
event EventHandler<GenericSingleEventArgs<string>>? DspLogicTriggerStateChanged
```

Triggered whenever a monitored trigger control changes. The event argument contains the ID of the trigger that changed.

---

## Methods

### AddDspLogicTrigger(string id, string tagName, List\<string\> tags)

```csharp
void AddDspLogicTrigger(string id, string tagName, List<string> tags)
```

Add a logic trigger control to the internal collection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the control. This is used for internal referencing. |
| `tagName` | `string` | The named control or tag name used in the DSP design. |
| `tags` | `List<string>` | A collection of any custom or informational tags associated with the control. |

---

### PulseDspLogicTrigger(string id)

```csharp
void PulseDspLogicTrigger(string id)
```

Activate a logic trigger control on the DSP device.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the trigger to activate. |
