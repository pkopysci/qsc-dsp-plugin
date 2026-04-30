// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-hardware-service/DeviceContainer.md
// Stub for the real type shipped in: gcu-hardware-service 4.3.4
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

using System.Collections.ObjectModel;

namespace gcu_hardware_service.BaseDevice;

public class DeviceContainer<T> : IDisposable
{
    public T? GetDevice(string id) => throw new NotImplementedException(
        "FrameworkStubs.DeviceContainer<T>.GetDevice — replace stub with real gcu-hardware-service package.");

    public ReadOnlyCollection<T> GetAllDevices() => throw new NotImplementedException(
        "FrameworkStubs.DeviceContainer<T>.GetAllDevices — replace stub with real gcu-hardware-service package.");

    public bool ContainsDevice(string id) => throw new NotImplementedException(
        "FrameworkStubs.DeviceContainer<T>.ContainsDevice — replace stub with real gcu-hardware-service package.");

    public void AddDevice(string id, T device) => throw new NotImplementedException(
        "FrameworkStubs.DeviceContainer<T>.AddDevice — replace stub with real gcu-hardware-service package.");

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        // No-op stub.
    }
}
