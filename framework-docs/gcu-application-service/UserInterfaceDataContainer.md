# UserInterfaceDataContainer

**Namespace:** `gcu_application_service.UserInterface`

**Inherits:** [`InfoContainer`](InfoContainer.md)

Data object for sending information about a touch panel or other user interface to subscribers.

---

## Table of Contents

**Constructors**
- [UserInterfaceDataContainer(...)](#userinterfacedatacontainer-1)

**Properties**
- [HelpContact](#helpcontact)
- [DefaultActivity](#defaultactivity)
- [SgdFile](#sgdfile)
- [IpId](#ipid)
- [ClassName](#classname)
- [Library](#library)
- [MenuItems](#menuitems)

**Methods**
- [AddMenuItem(MenuItemDataContainer item)](#addmenuitemmenuithemdatacontainer-item)

---

## Constructors

### UserInterfaceDataContainer(...)

```csharp
public UserInterfaceDataContainer(
    string id,
    string label,
    string helpContact,
    string icon,
    string model,
    string className,
    string library,
    string sgdFile,
    string defaultActivity,
    int ipId,
    List<string> tags)
```

Instantiates a new instance of `UserInterfaceDataContainer`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `id` | `string` | The unique ID of the interface. Used for internal referencing. |
| `label` | `string` | The user-friendly name of the interface or room name. |
| `helpContact` | `string` | The IT support phone number or other contact information to display on the UI. |
| `icon` | `string` | The image tag used for referencing the UI icon. |
| `model` | `string` | The specific model of the UI (e.g., `TSW-770`, `XPANEL`). |
| `className` | `string` | The name of the plugin class to instantiate. |
| `library` | `string` | The name of the plugin library used to instantiate a `className` object. |
| `sgdFile` | `string` | The smart graphics data file used when creating the UI interface. |
| `defaultActivity` | `string` | The default activity that should be displayed on the UI during startup. |
| `ipId` | `int` | The unique Crestron IP-ID used when connecting to the hardware. |
| `tags` | `List<string>` | A collection of custom tags used by the subscribed service. |

---

## Properties

### HelpContact

```csharp
public string HelpContact { get; }
```

The IT support phone number or other contact information displayed on the UI.

---

### DefaultActivity

```csharp
public string DefaultActivity { get; }
```

The default activity that should be displayed on the UI during startup.

---

### SgdFile

```csharp
public string SgdFile { get; }
```

The smart graphics data file used when creating the UI interface.

---

### IpId

```csharp
public int IpId { get; }
```

The unique Crestron IP-ID used when connecting to the hardware.

---

### ClassName

```csharp
public string ClassName { get; }
```

The plugin class name used to create a user interface object. If empty, the `Model` property is used to create a default interface.

---

### Library

```csharp
public string Library { get; }
```

The full name (including `.dll` extension) of the plugin library used to create a user interface object. If empty, the `Model` property is used to create a default interface.

---

### MenuItems

```csharp
public ReadOnlyCollection<MenuItemDataContainer> MenuItems { get; }
```

A read-only collection of menu data objects that will be displayed on the user interface.

---

## Methods

### AddMenuItem(MenuItemDataContainer item)

```csharp
public void AddMenuItem(MenuItemDataContainer item)
```

Add a menu item data object to the `MenuItems` collection.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `item` | `MenuItemDataContainer` | The menu item data object to add. Cannot be null. |
