# gcu-common-utils — Knowledge Base Index

A shared utility library for Crestron control system programs. Provides common abstractions for data management, file operations, event handling, IR/serial/network communications, logging, and input validation.

---

## Articles by Namespace

### DataObjects
| Article | Description |
|---------|-------------|
| [ListBuffer\<T\>](ListBuffer.md) | Thread-safe blocking buffer built on `List<T>`. |
| [Vector2D](Vector2D.md) | 2-dimensional vector data object with static directional constants. |

### FileOps
| Article | Description |
|---------|-------------|
| [DirectoryHelper](DirectoryHelper.md) | Static helpers for file path normalization and file existence checks on Crestron systems. |
| [DriverLoader](DriverLoader.md) | Reflection-based loader for Crestron Certified Driver (CCD) assemblies. |

### GenericEventArgs
| Article | Description |
|---------|-------------|
| [Generic Event Args](GenericEventArgs.md) | Typed `EventArgs` classes for passing 1, 2, or 3 data values with events. |

### IrComs
| Article | Description |
|---------|-------------|
| [IIrPort](IIrPort.md) | Interface contract for IR port implementations. |
| [CrestronIrPort](CrestronIrPort.md) | Crestron `IROutputPort` wrapper implementing `IIrPort`. |

### Logging
| Article | Description |
|---------|-------------|
| [Logger](Logger.md) | Static Serilog-based logging system with Crestron error log, console, and file sinks. |
| [Logging Types](LoggingTypes.md) | `LogServiceTypes` and `LogDeviceTypes` enumerations used in all log calls. |

### NetComs
| Article | Description |
|---------|-------------|
| [BasicFtpClient](BasicFtpClient.md) | SFTP client for querying and downloading files from remote servers. |
| [BasicTcpClient](BasicTcpClient.md) | Crestron TCP/IP client with event-driven data reception and auto-reconnect. |
| [TcpClientWrapper](TcpClientWrapper.md) | Async .NET `TcpClient` wrapper using callback delegates. |
| [WakeOnLan](WakeOnLan.md) | Sends Wake-On-LAN magic packets over LAN or control subnet. |

### SerialComs
| Article | Description |
|---------|-------------|
| [ISerialPort](ISerialPort.md) | Interface contract for serial comm port implementations. |
| [CrestronComPort](CrestronComPort.md) | Crestron `ComPort` wrapper implementing `ISerialPort`. |
| [CrestronComSpecHelper](CrestronComSpecHelper.md) | Converts config string/int values to Crestron `ComPort` enum types. |

### Validation
| Article | Description |
|---------|-------------|
| [DataFormatter](DataFormatter.md) | String normalization utilities for configuration argument standardization. |
| [ParameterValidator](ParameterValidator.md) | Guard methods that throw on null or empty method arguments. |
