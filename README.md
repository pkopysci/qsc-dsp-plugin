# qsc-dsp-plugin
AV Framework hardware plugin for QSC Q-SYS DSP control using the QRC protocol. This library will be a plug-in for an existing AV System framework. It will implement the required APIs listed below to allow control of any Q-SYS audio DSP hardware.

The plugin will be loaded at runtime by the [AV Framework infrastructure service](/framework-docs/gcu-hardware-service/InfrastructureService.md). See The [Framework Documentation](/framework-docs/README.md) documentation for boot sequence, plugin loading behavior, and general documentation on the full AV Framework.

## Development & Deployment Environment
- Must use .NET 8.0
- Project must be a .NET 8.0 Class Library
- Will run on a [Crestron RMC4 Processor](https://www.crestron.com/Products/Catalog/Control-and-Management/Control-System/Wall-Mount/RMC4)

## Dependencies
### 1. Framework Packages
All framework libries that are required can be found in the **./dependencies** directory of this repository.

**The *gcu-hardware-service and gcu-common-utils* libraries are required for API implementation.**

### 2. Allowed 3rd Party NuGet Packages
The following packages are allowed if necessary. Do not install any dependencies that are not included, or required by, libaries in this list.

- [Newtonsoft.Json 13.0.3](https://www.nuget.org/packages/Newtonsoft.Json/13.0.3)
- [Crestron SimplSharp.SDK.ProgramLibrary 2.21.237](https://www.nuget.org/packages/Crestron.SimplSharp.SDK.ProgramLibrary/#readme-body-tab)
- all packages listed in the [dependencies](/dependencies/) directory.


## Library Requirements

### 1. Project strucure and names
- The library must be named **QscDspDevices**
- The root namespace must be **QscDspDevices**
- The root public class must be named **QscDspTcp**
- All other design choices must follow [Microsoft Conventions](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/). No exceptions unless one of the [white listed](#2-allowed-3rd-party-nuget-packages) dependencies does not allow it.

### 2. Documentation & Logging
- All errors, warnings, and debug statements must be logged using the *gcu_common_utils.Logging.Logger* class.
- All public and protected classes and class members must be fully documented using [C# XML doc comment (/// format)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments). This documentation must include a brief description of the class or member, parameter definitions, return value definitions, and any exceptions that are thrown.

### 3. API Implementation

#### Q-SYS Control Protocol
This library must use QRC (and ECP when appropriate) commands to control the DSP device. Documentation on this APi can be found in the [QSC External Controls Documentation](hhttps://help.qsys.com/q-sys_9.8/Content/External_Control_APIs/QRC/QRC_Overview.htm).

#### Minimum Interface Implementation
The *QscDspTcp* class must implement the following interfaces from the *gcu_hardware_service* library:

```csharp
public abstract class gcu_hardware_service.BaseDevice.BaseDevice
```
- `BaseDevice.Manufacturer` must be set to *QSC*
- `BaseDevice.Model` must be set to *Q-SYS Core*
- `BaseDevice.Connect()` and `BaseDevice.Disconnect()` must be overriden and used to establish/disconnect a TCP/IP connection with the hardware.
- `BaseDevice.IsOnline` must be set when the connection to the hardware changes. `BaseDevice.NotifyOnlineStatus()` Must be called **after** IsOnline has been set.


```csharp
public interface gcu_hardware_service.AudioDevices.IDsp
```
```csharp
public interface gcu_hardware_service.AudioDevices.IAudioRoutable
```
```csharp
public interface gcu_hardware_service.AudioDevices.IAudioZoneEnabler
```
```csharp
public interface gcu_hardware_service.AudioDevices.IRedundanceSupport
```
```csharp
public interface gcu_hardware_service.AudioDevices.IDspLogicTriggerSupport
```

### 4. Restrictions
- Internal thread count must be limitted to a maximum of 3 concurrent threads.
- The library must be non-blocking and all threading must be managed internally (no public async/await).
- The library must use the `gcu_common_utils.NetComs.BasicTcpClient` for connecting to the hardware.
- All classes, including internal or private classes, must implement IDisposable when necessary and follow the [recommended disposal pattern](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable.dispose?view=net-8.0).
- The library must compile without errors or warnings. Warnings from external libraries are allowed; there is a known warning from Crestron libraries that is unavoidable but does not affect functionality.
- The release DLL must not exceed 500KB in size.

### 5. Scope of Work

#### Basic Features:
- zero to many audio presets
- zero to many audio input channels
- zero to many audio output channels
- matrix routing for all output channels (any input to any output) if the router named control has been defined
- zero to one redundant Q-SYS Core for fail-over if the primary DSP loses connection. The library must switch back to the primary once it comes back online and is connected to a backup device.

#### Q-SYS specific Component Support
The library must support the following components of a Q-SYS DSP integration (See [QSC External Controls Documentation](https://help.qsys.com/q-sys_9.8/Content/External_Control_APIs/QRC/QRC_Overview.htm)):
- Q-SYS Named Controls for audio routing, gain/mute controls, and logic triggers (Control.Get & Control.Set)
- Numeric values for channel or mixer gain controls
- boolean values for channel or mixer mute states
- numeric values for matrix routing blocks
- numeric, boolean, and string Q-SYS Named Controls assiciated with the gain, mute, and router named controls.
- Snapshots when adding or recalling the `IAudioControl.RecallAudioPreset()` and `IAudioControl.AddAudioPreset()` functionality.

#### Device Connection
- The library will maintain a connection with the DSP until `BaseDevice.Disconnect()` is called externally.
- If connection is lost unexpectedly, the library will immediately attempt to reconnect if there is no backup device. Otherwise it will switch to the backup device following the recommended practices defined in the Q-Sys QRC documentation.
- On a failed connection the library will log an error to the Logger, wait 15 seconds, and attempt to reconnect to the device. This will be repeated until `BaseDevice.Disconnect()` is called externally or a connection is established.
- on any disconnect event the `BaseDevice.IsOnline` property must be updated and `BaseDevice.NotifyOnlineStatus()` must be called.
- Upon a successfull connection, the library will update the state of all registered channels, routing, logic states, and active snapshots(presets).

#### Sending/Receiving commands
- Commands must be sent as soon as possible
- If a command request is triggered while sending a command, the new command must be queued and send at the next opportunity
- sending commands is FIFO (First In First Out)
- Do not attempt to send or queue commands if there is no connection to a primary or backup device. An error must be logged to the Logger if an attempt to send a command is made while disconnected or not initialized.
- On any disconnect event the command queue must be cleared
- The library will maintain an up-to-date state of all registered controls, either through polling or subscribing to QRC/ECP events.
- when any state update is received from the device, the related control state must be updated internally and subscribers must be notified after the control has been updated. For example, if an output gain value was changed, the associated object will be updated and then `AudioOutputLevelChanged` will be invoked.

#### Exception Handling
- Avoid throwing unhandled exceptions unless absolutely necessary or explicitly instructed to do so by the AV Framework documentation
- The library must not fail at any point that causes a system crash
- All exceptions or logic errors must be recorded as Errors using the `gcu_common_utils.Logging.Logger` class.
