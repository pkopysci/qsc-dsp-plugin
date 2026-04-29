# gcu-config-service

Documentation for the `gcu-config-service` library. This library is responsible for locating and parsing the room configuration JSON file at program startup, verifying that all referenced plugin dependencies are present on the control processor, and downloading any missing files via SFTP before signaling to `gcu-avf` that the system is ready to initialize.

---

## Core

| Type | Description |
|------|-------------|
| [ConfigurationService](ConfigurationService.md) | Loads the room configuration JSON, resolves plugin dependencies, and raises `ConfigLoadComplete` or `ConfigLoadFailed` when finished. |
