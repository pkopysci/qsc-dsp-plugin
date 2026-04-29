# IAudioPresetApp

**Namespace:** `gcu_application_service.AudioControl`

Required methods for an application service that supports managing audio presets on DSP devices.

---

## Table of Contents

**Methods**
- [QueryDspAudioPresets(string dspId)](#querydspaudiopresetsstring-dspid)
- [RecallAudioPreset(string dspId, string presetId)](#recallaudiopresetstring-dspid-string-presetid)

---

## Methods

### QueryDspAudioPresets(string dspId)

```csharp
ReadOnlyCollection<InfoContainer> QueryDspAudioPresets(string dspId)
```

Get a collection of all audio presets that can be recalled on the target DSP.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `dspId` | `string` | The unique ID of the DSP to query. |

**Returns:** All preset info objects for that DSP. Returns an empty collection if `dspId` is not found.

---

### RecallAudioPreset(string dspId, string presetId)

```csharp
void RecallAudioPreset(string dspId, string presetId)
```

Send a preset/snapshot recall request to the target DSP.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `dspId` | `string` | The unique ID of the DSP to control. |
| `presetId` | `string` | The unique ID of the preset associated with the DSP that will be recalled. |
