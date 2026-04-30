// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-hardware-service/IBaseDevice.md
// Stub for the real type shipped in: gcu-hardware-service 4.3.4
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

using gcu_common_utils.GenericEventArgs;

namespace gcu_hardware_service.BaseDevice;

public interface IBaseDevice
{
    event EventHandler<GenericSingleEventArgs<string>> ConnectionChanged;

    string Id
    {
        get;
    }

    string Label
    {
        get;
    }

    bool IsOnline
    {
        get;
    }

    bool IsInitialized
    {
        get;
    }

    string Manufacturer
    {
        get; set;
    }

    string Model
    {
        get; set;
    }

    void Connect();

    void Disconnect();
}
