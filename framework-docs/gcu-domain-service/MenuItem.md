# MenuItem

**Namespace:** `gcu_domain_service.Data.UserInterfaceData`

**Inherits:** [`BaseData`](BaseData.md)

Configuration data for a single UI menu control item. Inherits `Id`, `Manufacturer`, and `Model` from `BaseData`.

---

## Table of Contents

**Properties**
- [Visible](#visible)
- [Label](#label)
- [Icon](#icon)
- [Control](#control)
- [SourceSelect](#sourceselect)
- [Tags](#tags)

---

## Properties

### Visible

```csharp
public bool Visible { get; set; }
```

**Type:** `bool`

Gets or sets a value indicating whether this menu item should be visible on the UI. Defaults to `false`.

---

### Label

```csharp
public string Label { get; set; }
```

**Type:** `string`

Gets or sets the label to display on the UI for this control. Defaults to `string.Empty`.

---

### Icon

```csharp
public string Icon { get; set; }
```

**Type:** `string`

Gets or sets the icon to display on the UI for this control. Defaults to `string.Empty`.

---

### Control

```csharp
public string Control { get; set; }
```

**Type:** `string`

Gets or sets the control or activity identifier that will be displayed when the menu item is selected. Defaults to `string.Empty`.

---

### SourceSelect

```csharp
public string SourceSelect { get; set; }
```

**Type:** `string`

Gets or sets the ID of the [`Source`](Source.md) to route when the menu item is selected. Can be the empty string (`""`) if no source routing is required. Defaults to `string.Empty`.

---

### Tags

```csharp
public List<string> Tags { get; set; }
```

**Type:** `List<string>`

A collection of string tags used to define or filter special behaviors for this menu item. Defaults to an empty list.
