// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-common-utils/BasicTcpClient.md
// Stub for the real type shipped in: gcu-common-utils 4.3.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

using Crestron.SimplSharp.CrestronSockets;
using gcu_common_utils.GenericEventArgs;

namespace gcu_common_utils.NetComs;

public class BasicTcpClient : IDisposable
{
    public BasicTcpClient(string hostname = "localhost", int port = 80, int bufferSize = 5000)
    {
        // Per framework-docs/gcu-common-utils/BasicTcpClient.md "Exceptions":
        //   ArgumentNullException — hostname null or empty
        //   ArgumentException     — port outside 0..65535 OR bufferSize < 0
        // Validation is part of the documented constructor contract, so the
        // stub MUST reproduce it; otherwise tests pass against the stub and
        // throw against the real DLL.
        if (string.IsNullOrEmpty(hostname))
        {
            throw new ArgumentNullException(nameof(hostname));
        }

        if (port < 0 || port > 65535)
        {
            throw new ArgumentException("Port must be in the range 0..65535.", nameof(port));
        }

        if (bufferSize < 0)
        {
            throw new ArgumentException("Buffer size must be non-negative.", nameof(bufferSize));
        }

        Hostname = hostname;
        Port = port;
        BufferSize = bufferSize;
        RxData = string.Empty;
        RxBytes = Array.Empty<byte>();
        ReconnectTime = 30000;
    }

    public event EventHandler<GenericSingleEventArgs<SocketStatus>>? ConnectionFailed;

    public event EventHandler? ClientConnected;

    public event EventHandler? StatusChanged;

    public event EventHandler<GenericSingleEventArgs<string>>? RxReceived;

    public event EventHandler<GenericSingleEventArgs<byte[]>>? RxBytesReceived;

    public string Hostname
    {
        get;
    }

    public string RxData
    {
        get;
    }

    public byte[] RxBytes
    {
        get;
    }

    public SocketStatus ClientStatusMessage => throw new NotImplementedException(
        "FrameworkStubs.BasicTcpClient.ClientStatusMessage — replace stub with real gcu-common-utils package.");

    public int Port
    {
        get;
    }

    public int BufferSize
    {
        get;
    }

    public bool Connected => throw new NotImplementedException(
        "FrameworkStubs.BasicTcpClient.Connected — replace stub with real gcu-common-utils package.");

    public bool EnableReconnect
    {
        get; set;
    }

    public int ReconnectTime
    {
        get; set;
    }

    public void Connect() => throw new NotImplementedException(
        "FrameworkStubs.BasicTcpClient.Connect — replace stub with real gcu-common-utils package.");

    public void Disconnect() => throw new NotImplementedException(
        "FrameworkStubs.BasicTcpClient.Disconnect — replace stub with real gcu-common-utils package.");

    public void Send(string data) => throw new NotImplementedException(
        "FrameworkStubs.BasicTcpClient.Send(string) — replace stub with real gcu-common-utils package.");

    public void Send(byte[] data) => throw new NotImplementedException(
        "FrameworkStubs.BasicTcpClient.Send(byte[]) — replace stub with real gcu-common-utils package.");

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        // No-op stub. CS0067 (event declared but never raised) is suppressed
        // by csproj <NoWarn> for this stub assembly; no extra discards needed.
    }
}
