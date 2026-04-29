# IPresetDevice

**Namespace:** `gcu_hardware_service.CameraDevices`

Minimum required properties and methods for a device that supports preset features, usually used by PTZ cameras.

---

## Table of Contents

**Properties**
- [SupportsSavingPresets](#supportssavingpresets)

**Methods**
- [QueryAllPresets()](#queryallpresets)
- [SetPresetData(List\<CameraPreset\> presets)](#setpresetdatalistcamerapreset-presets)
- [RecallPreset(string id)](#recallpresetstring-id)
- [SavePreset(string id)](#savepresetstring-id)

---

## Properties

### SupportsSavingPresets

```csharp
bool SupportsSavingPresets { get; }
```

`true` = this device supports saving preset states from 3rd party controls; `false` = no save support.

---

## Methods

### QueryAllPresets()

```csharp
ReadOnlyCollection<CameraPreset> QueryAllPresets()
```

Returns a collection of all presets configured on the device hardware.

**Returns:** A read-only collection of [`CameraPreset`](CameraPreset.md) data objects.

---

### SetPresetData(List\<CameraPreset\> presets)

```csharp
void SetPresetData(List<CameraPreset> presets)
```

Update the internally stored collection of presets.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `presets` | `List<CameraPreset>` | The collection of preset data to store, typically created from a framework configuration file. |

---

### RecallPreset(string id)

```csharp
void RecallPreset(string id)
```

Send a command to the device to recall the target preset state.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the preset to recall. |

---

### SavePreset(string id)

```csharp
void SavePreset(string id)
```

Save the current device position or state to the target preset. Only applicable when `SupportsSavingPresets` is `true`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The ID of the preset to create or overwrite. |
