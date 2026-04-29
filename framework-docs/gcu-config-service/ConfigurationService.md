# ConfigurationService

**Namespace:** `gcu_config_service`

**Implements:** `IDisposable`

Sealed class for loading room configuration data from a JSON file and ensuring all required plugin dependencies are present on the control processor. On a successful load, the `Domain` property is populated with an `IDomainService` built from the configuration file. If any plugin `.dll` files referenced in the configuration are missing from the local file system, the service will attempt to download them from the server defined in the configuration's `ServerInfo` block via SFTP. After a successful download the Crestron program slot is automatically restarted to load the new files.

The full load sequence initiated by `LoadConfig()` is:

1. Locate the JSON config file matching the program slot in the user folder.
2. Parse the JSON and create the `IDomainService` via `DomainFactory`.
3. Scan the local plugin directory (`/user/net8-plugins/`) for missing dependencies.
4. If all dependencies are present, raise `ConfigLoadComplete`.
5. If any are missing, connect to the SFTP server and download them sequentially.
6. After all downloads complete, issue a `progreset` console command to restart the program slot so the new files are loaded.

---

## Table of Contents

**Constructors**
- [ConfigurationService(uint programSlot)](#configurationserviceuint-programslot)

**Events**
- [ConfigLoadComplete](#configloadcomplete)
- [ConfigLoadFailed](#configloadfailed)

**Properties**
- [Domain](#domain)

**Methods**
- [LoadConfig()](#loadconfig)
- [Dispose()](#dispose)

---

## Constructors

### ConfigurationService(uint programSlot)

```csharp
public ConfigurationService(uint programSlot)
```

Instantiates a new instance of `ConfigurationService`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `programSlot` | `uint` | The Crestron program slot number used to locate the matching configuration file (e.g., slot `1` matches `*slot01_config*.json`). |

---

## Events

### ConfigLoadComplete

```csharp
public event EventHandler<EventArgs>? ConfigLoadComplete
```

Triggered when all dependencies have been verified and loaded into the program. Subscribe to this event to proceed with infrastructure and application service initialization.

---

### ConfigLoadFailed

```csharp
public event EventHandler<EventArgs>? ConfigLoadFailed
```

Triggered if there was a failure at any point during the load process — including a missing config file, a JSON parse error, a failed dependency check, or a failed SFTP download.

---

## Properties

### Domain

```csharp
public IDomainService? Domain { get; }
```

Gets the domain hardware management service created from the configuration file. This property is `null` until `ConfigLoadComplete` has been raised.

---

## Methods

### LoadConfig()

```csharp
public void LoadConfig()
```

Load all dependency information and create the domain service. Searches the Crestron user folder for a JSON file matching the pattern `*slot##_config*.json`, parses it into an `IDomainService`, then checks the local plugin directory for any missing `.dll` or `.sgd` files referenced in the configuration. If any are missing, initiates an SFTP download sequence before raising `ConfigLoadComplete`. Raises `ConfigLoadFailed` if the config file cannot be found, parsed, or if a required dependency cannot be downloaded.

---

### Dispose()

```csharp
public void Dispose()
```

Releases all managed resources, including the internal SFTP client connection if one is active.
