// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-hardware-service/IRedundancySupport.md
// Stub for the real type shipped in: gcu-hardware-service 4.3.4
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

using gcu_common_utils.GenericEventArgs;

namespace gcu_hardware_service.Redundancy;

public interface IRedundancySupport
{
    event EventHandler<GenericSingleEventArgs<string>> RedundancyStateChanged;

    event EventHandler<GenericSingleEventArgs<string>> BackupDeviceConnectionChanged;

    bool PrimaryDeviceActive
    {
        get;
    }

    bool BackupDeviceActive
    {
        get;
    }

    bool BackupDeviceOnline
    {
        get;
    }

    bool BackupDeviceExists
    {
        get;
    }

    void SetBackupDeviceConnection(string hostname, int port);
}
