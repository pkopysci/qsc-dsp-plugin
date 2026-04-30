// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using gcu_common_utils.GenericEventArgs;
using gcu_common_utils.NetComs;

namespace QscDspDevices.Transport;

/// <summary>
/// Production <see cref="IConnectionTransport"/> wrapping the framework's
/// <c>gcu_common_utils.NetComs.BasicTcpClient</c>. README §4 makes this
/// the only sanctioned TCP client for production use; <c>System.Net.Sockets.TcpClient</c>
/// is restricted on the Crestron AppDomain at runtime.
/// </summary>
/// <remarks>
/// <para>
/// The wrapper subscribes to the underlying client's events and re-raises
/// them through the abstraction. It also sets <c>EnableReconnect = false</c>
/// because the plugin's <c>ConnectionManager</c> owns the reconnect policy
/// (15 seconds, fixed) — the framework client's built-in reconnect would
/// race with our state machine.
/// </para>
/// <para>
/// During tests, this type is exercised via Moq against a fake
/// <c>BasicTcpClient</c>; integration tests use <c>RawTcpTransport</c>
/// from TestSupport instead so they never touch the framework stub.
/// </para>
/// </remarks>
public sealed class BasicTcpClientTransport : IConnectionTransport
{
    private readonly BasicTcpClient _client;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicTcpClientTransport"/> class
    /// that connects to the supplied hostname and port.
    /// </summary>
    /// <param name="hostname">The IP address or hostname.</param>
    /// <param name="port">The TCP port (0..65535).</param>
    /// <param name="bufferSize">Read/write buffer size in bytes.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="hostname"/> is null or empty.</exception>
    /// <exception cref="ArgumentException">If <paramref name="port"/> is outside 0..65535 or <paramref name="bufferSize"/> is negative.</exception>
    public BasicTcpClientTransport(string hostname, int port, int bufferSize = 8192)
    {
        _client = new BasicTcpClient(hostname, port, bufferSize)
        {
            // Plugin's ConnectionManager owns reconnect policy; framework
            // client's built-in reconnect would race and double-up attempts.
            EnableReconnect = false,
        };

        _client.ClientConnected += OnClientConnected;
        _client.ConnectionFailed += OnConnectionFailed;
        _client.RxBytesReceived += OnRxBytesReceived;
    }

    /// <inheritdoc />
    public event EventHandler<EventArgs>? Connected;

    /// <inheritdoc />
    public event EventHandler<GenericSingleEventArgs<string>>? ConnectionFailed;

    /// <inheritdoc />
    public event EventHandler<GenericSingleEventArgs<ReadOnlyMemory<byte>>>? RxReceived;

    /// <inheritdoc />
    public bool IsConnected => _client.Connected;

    /// <inheritdoc />
    public void Connect()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _client.Connect();
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        if (_disposed)
        {
            return;
        }

        _client.Disconnect();
    }

    /// <inheritdoc />
    public void Send(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_client.Connected)
        {
            throw new InvalidOperationException("Transport is not connected.");
        }

        _client.Send(payload);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _client.ClientConnected -= OnClientConnected;
        _client.ConnectionFailed -= OnConnectionFailed;
        _client.RxBytesReceived -= OnRxBytesReceived;

        // Note: do NOT clear Connected/ConnectionFailed/RxReceived to null.
        // The unsubscribes above remove our internal handlers. Setting the
        // public events to null does not free subscribers' references; it
        // only changes our local view, and is confusing to readers.
        _client.Dispose();
    }

    private void OnClientConnected(object? sender, EventArgs e)
        => Connected?.Invoke(this, EventArgs.Empty);

    private void OnConnectionFailed(object? sender, GenericSingleEventArgs<Crestron.SimplSharp.CrestronSockets.SocketStatus> e)
        => ConnectionFailed?.Invoke(this, new GenericSingleEventArgs<string>(e.Arg.ToString()));

    private void OnRxBytesReceived(object? sender, GenericSingleEventArgs<byte[]> e)
        => RxReceived?.Invoke(this, new GenericSingleEventArgs<ReadOnlyMemory<byte>>(e.Arg));
}
