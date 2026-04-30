// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-hardware-service/BaseDevice.md
// Stub for the real type shipped in: gcu-hardware-service 4.3.4
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

using gcu_common_utils.GenericEventArgs;

namespace gcu_hardware_service.BaseDevice;

public abstract class BaseDevice : IBaseDevice
{
    public event EventHandler<GenericSingleEventArgs<string>>? ConnectionChanged;

    public string Id { get; protected set; } = string.Empty;

    public string Label { get; protected set; } = string.Empty;

    public virtual bool IsOnline
    {
        get; protected set;
    }

    public virtual bool IsInitialized
    {
        get; protected set;
    }

    public string Manufacturer { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public virtual void Connect()
    {
        // Base implementation is a no-op; override in subclasses.
    }

    public virtual void Disconnect()
    {
        // Base implementation is a no-op; override in subclasses.
    }

    protected virtual void NotifyOnlineStatus()
    {
        // Reference the event so the stub compiles even when no subscribers exist.
        ConnectionChanged?.Invoke(this, new GenericSingleEventArgs<string>(Id));
    }
}
