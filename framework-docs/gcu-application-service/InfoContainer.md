# InfoContainer

**Namespace:** `gcu_application_service.Base`

Base data container class containing common attributes for all device data objects in the application service.

---

## Table of Contents

**Properties**
- [Empty](#empty)
- [Id](#id)
- [Label](#label)
- [Manufacturer](#manufacturer)
- [Model](#model)
- [Icon](#icon)
- [IsOnline](#isonline)
- [Tags](#tags)

**Constructors**
- [InfoContainer(...)](#infocontainer-1)

---

## Properties

### Empty

```csharp
public static readonly InfoContainer Empty
```

A default, empty data object with ID `"EMPTYINFO"` and label `"EMPTY INFO"`.

---

### Id

```csharp
public string Id { get; protected set; }
```

The unique ID of this data item, used for internal referencing.

---

### Label

```csharp
public string Label { get; protected set; }
```

The user-friendly label of this data item.

---

### Manufacturer

```csharp
public string Manufacturer { get; init; }
```

The company that manufactures the represented device. Defaults to the empty string.

---

### Model

```csharp
public string Model { get; init; }
```

The device name as defined by the manufacturer. Defaults to the empty string.

---

### Icon

```csharp
public string Icon { get; protected set; }
```

The icon key for this data item, used by UI implementations to display an image.

---

### IsOnline

```csharp
public bool IsOnline { get; set; }
```

`true` if the device is currently online; `false` if offline.

---

### Tags

```csharp
public List<string> Tags { get; protected set; }
```

A collection of custom tags associated with this data item. Usage is determined by the subscribing service.

---

## Constructors

### InfoContainer(...)

```csharp
public InfoContainer(string id, string label, string icon, List<string> tags, bool isOnline = false)
```

Instantiates a new instance of `InfoContainer`.

**Parameters**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `id` | `string` | | The unique ID of the device. Used for internal referencing. |
| `label` | `string` | | The user-friendly name of the device. |
| `icon` | `string` | | The image tag used for referencing the UI icon. |
| `tags` | `List<string>` | | A collection of custom tags used by the subscribed service. |
| `isOnline` | `bool` | `false` | `true` = device is currently connected, `false` = device offline. |
