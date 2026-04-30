// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-hardware-service/IDsp.md
// Stub for the real type shipped in: gcu-hardware-service 4.3.4
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

namespace gcu_hardware_service.AudioDevices;

public interface IDsp : IAudioControl, IDisposable
{
    void Initialize(
        string hostId,
        int coreId,
        string hostname,
        int port,
        string username,
        string password);
}
