// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-common-utils/TcpClientWrapper.md
// Stub for the real type shipped in: gcu-common-utils 4.3.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

namespace gcu_common_utils.NetComs;

public class TcpClientWrapper : IDisposable
{
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public string IpAddress { get; init; } = "127.0.0.1";

    public int Port { get; init; } = 80;

    public bool IsConnected => throw new NotImplementedException(
        "FrameworkStubs.TcpClientWrapper.IsConnected — replace stub with real gcu-common-utils package.");

    public Func<TcpClientWrapper, Task>? OnConnectedCallback
    {
        get; init;
    }

    public Func<TcpClientWrapper, bool, Task>? OnDisconnectedCallback
    {
        get; init;
    }

    public Func<TcpClientWrapper, byte[], Task>? OnDataReceivedCallback
    {
        get; init;
    }

    public Action<TcpClientWrapper, string>? OnConnectionFailedCallback
    {
        get; init;
    }

    public Task ConnectAsync() => throw new NotImplementedException(
        "FrameworkStubs.TcpClientWrapper.ConnectAsync — replace stub with real gcu-common-utils package.");

    public void Disconnect() => throw new NotImplementedException(
        "FrameworkStubs.TcpClientWrapper.Disconnect — replace stub with real gcu-common-utils package.");

    public Task SendAsync(byte[] data, CancellationToken cancellationToken = default) => throw new NotImplementedException(
        "FrameworkStubs.TcpClientWrapper.SendAsync — replace stub with real gcu-common-utils package.");

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
