# gcu-domain-service — Knowledge Base Index

A domain service library for Crestron control systems. Provides a strongly-typed, read-only view of the JSON system configuration file, enabling hardware drivers and application services to query device data by type and ID.

---

## Articles by Namespace

### Core Service
| Article | Description |
|---------|-------------|
| [IDomainService](IDomainService.md) | Interface contract for the domain hardware provider service. |
| [DomainService](DomainService.md) | `IDomainService` implementation backed by a `DataContainer`. |
| [DomainFactory](DomainFactory.md) | Static factory for constructing an `IDomainService` from JSON. |

### Transports
| Article | Description |
|---------|-------------|
| [TransportCommands](TransportCommands.md) | Enum of generic transport commands for Blu-ray, DVD, and TV tuner devices. |

### Data — Root Models
| Article | Description |
|---------|-------------|
| [BaseData](BaseData.md) | Base class inherited by all configuration data objects. Provides `Id`, `Manufacturer`, and `Model`. |
| [DataContainer](DataContainer.md) | Root deserialization target for the JSON configuration file. |
| [ServerInfo](ServerInfo.md) | Remote dependency server (SFTP) connection credentials. |

### Data — CameraData
| Article | Description |
|---------|-------------|
| [Camera](Camera.md) | Configuration data for a single camera device. |
| [PresetData](PresetData.md) | Configuration data for a single camera preset position. |

### Data — ConnectionData
| Article | Description |
|---------|-------------|
| [Connection](Connection.md) | TCP/IP, RS-232, or IR connection configuration for a device. |
| [Authentication](Authentication.md) | Username/password credentials for TCP device login. |
| [ComSpec](ComSpec.md) | RS-232/RS-422/RS-485 serial communication parameters. |

### Data — DisplayData
| Article | Description |
|---------|-------------|
| [Display](Display.md) | Configuration data for a single display device. |
| [CustomCommands](CustomCommands.md) | Custom protocol strings for display freeze operations. |

### Data — DriverData
| Article | Description |
|---------|-------------|
| [UserAttribute](UserAttribute.md) | Driver-specific key/value configuration attribute. |

### Data — DspData
| Article | Description |
|---------|-------------|
| [Audio](Audio.md) | Container for DSP devices and audio channel configurations. |
| [Dsp](Dsp.md) | Configuration data for a single DSP device. |
| [Channel](Channel.md) | Configuration data for a single DSP audio channel. |
| [Preset (DspData)](DspPreset.md) | Configuration data for a single DSP preset/scene recall. |
| [LogicTrigger](LogicTrigger.md) | Configuration data for a DSP logic trigger tag. |
| [ZoneEnableToggle](ZoneEnableToggle.md) | DSP zone enable/disable toggle control data. |

### Data — EndpointData
| Article | Description |
|---------|-------------|
| [Endpoint](Endpoint.md) | Configuration data for a remote AV endpoint device (hosts serial, IR, and relay ports). |

### Data — FusionData
| Article | Description |
|---------|-------------|
| [FusionInfo](FusionInfo.md) | Crestron Fusion room registration configuration. |

### Data — LightingData
| Article | Description |
|---------|-------------|
| [LightingInfo](LightingInfo.md) | Configuration data for a single lighting controller. |
| [LightingAttribute](LightingAttribute.md) | Configuration data for a single lighting zone or scene. |

### Data — RoomInfoData
| Article | Description |
|---------|-------------|
| [RoomInfo](RoomInfo.md) | Basic room identification, system type, and shutdown schedule. |
| [Logic](Logic.md) | Application and presentation service plug-in assignments. |

### Data — RoutingData
| Article | Description |
|---------|-------------|
| [Routing](Routing.md) | Root AV routing configuration: sources, destinations, matrices, and graph edges. |
| [Source](Source.md) | Configuration data for a single AV routing source. |
| [Destination](Destination.md) | Configuration data for a single AV routing destination (output). |
| [MatrixData](MatrixData.md) | Configuration data for a single AV matrix switcher. |
| [MatrixEdge](MatrixEdge.md) | A directed edge between two nodes in the routing graph. |

### Data — TransportDeviceData
| Article | Description |
|---------|-------------|
| [Bluray](Bluray.md) | Configuration data for a single Blu-ray player. |
| [CableBox](CableBox.md) | Configuration data for a single cable/satellite box. |
| [TransportFavorite](TransportFavorite.md) | A favorite channel entry for a cable/satellite box. |

### Data — UserInterfaceData
| Article | Description |
|---------|-------------|
| [UserInterface](UserInterface.md) | Configuration data for a single UI panel. |
| [MenuItem](MenuItem.md) | Configuration data for a single UI main menu item. |

### Data — VideoWallData
| Article | Description |
|---------|-------------|
| [VideoWall](VideoWall.md) | Configuration data for a single video wall controller. |
