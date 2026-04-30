// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-hardware-service/ITcpDevice.md
// Stub for the real type shipped in: gcu-hardware-service 4.3.4
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

namespace gcu_hardware_service.Communication;

public interface ITcpDevice
{
    void SetTcpConnectionInfo(string host, int port);
}
