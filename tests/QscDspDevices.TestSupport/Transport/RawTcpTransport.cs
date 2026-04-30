// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using gcu_common_utils.GenericEventArgs;
using QscDspDevices.Transport;

namespace QscDspDevices.TestSupport.Transport;

/// <summary>
/// Test-only <see cref="IConnectionTransport"/> implementation backed by
/// <see cref="System.Net.Sockets.TcpClient"/> directly. Used by integration
/// tests to talk to the in-process <c>FakeQrcServer</c> without invoking
/// <c>BasicTcpClient</c> (which is stubbed in <c>FrameworkStubs</c> and
/// would throw <see cref="System.NotImplementedException"/>).
/// </summary>
/// <remarks>
/// This transport is intentionally simple: spin a background read loop on
/// connect, push raw bytes through <see cref="RxReceived"/>, and report
/// any I/O exception as a <see cref="ConnectionFailed"/>. The integration
/// tests rely on this fidelity to exercise the framer and dispatcher
/// against real bytes-on-the-wire from the FakeQrcServer.
/// </remarks>
public sealed class RawTcpTransport : IConnectionTransport
{
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private Task? _readLoop;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RawTcpTransport"/> class
    /// pointing at the supplied endpoint.
    /// </summary>
    /// <param name="host">The remote hostname or IP.</param>
    /// <param name="port">The remote TCP port.</param>
    public RawTcpTransport(string host, int port)
    {
        _host = host;
        _port = port;
    }

    /// <inheritdoc />
    public event EventHandler<EventArgs>? Connected;

    /// <inheritdoc />
    public event EventHandler<GenericSingleEventArgs<string>>? ConnectionFailed;

    /// <inheritdoc />
    public event EventHandler<GenericSingleEventArgs<ReadOnlyMemory<byte>>>? RxReceived;

    /// <inheritdoc />
    public bool IsConnected => _client?.Connected == true;

    /// <inheritdoc />
    public void Connect()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_client is not null)
        {
            return;
        }

        _client = new TcpClient();
        _cts = new CancellationTokenSource();

        // Connect on a background task so Connect() returns promptly per
        // the IConnectionTransport contract.
        _ = Task.Run(async () =>
        {
            try
            {
                await _client.ConnectAsync(_host, _port).ConfigureAwait(false);
                _stream = _client.GetStream();
                Connected?.Invoke(this, EventArgs.Empty);
                _readLoop = Task.Run(() => ReadLoopAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                ConnectionFailed?.Invoke(this, new GenericSingleEventArgs<string>(ex.GetType().Name + ": " + ex.Message));
            }
        });
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed; no-op.
        }

        try
        {
            _stream?.Close();
        }
        catch (System.IO.IOException)
        {
            // Closing a NetworkStream that already lost its socket can
            // throw IOException; we are tearing down on purpose.
        }
        catch (System.Net.Sockets.SocketException)
        {
            // Same rationale as IOException.
        }
        catch (ObjectDisposedException)
        {
            // The stream was already disposed; nothing to do.
        }

        try
        {
            _client?.Close();
        }
        catch (System.IO.IOException)
        {
            // Same rationale as above for the underlying TcpClient.
        }
        catch (System.Net.Sockets.SocketException)
        {
            // Same rationale as IOException.
        }
        catch (ObjectDisposedException)
        {
            // Already disposed.
        }
    }

    /// <inheritdoc />
    public void Send(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ObjectDisposedException.ThrowIf(_disposed, this);

        NetworkStream stream = _stream
            ?? throw new InvalidOperationException("Transport is not connected.");
        stream.Write(payload, 0, payload.Length);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        Disconnect();

        try
        {
            _readLoop?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Read loop may have surfaced an aggregate-wrapped IO error
            // when the socket was torn down; we don't propagate shutdown
            // noise to the test runner.
        }
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            // Cancellation during teardown — expected.
        }

        _cts?.Dispose();
        _stream?.Dispose();
        _client?.Dispose();

        Connected = null;
        ConnectionFailed = null;
        RxReceived = null;
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        NetworkStream? stream = _stream;
        if (stream is null)
        {
            return;
        }

        byte[] buffer = new byte[16 * 1024];
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int n;
                try
                {
                    n = await stream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    ConnectionFailed?.Invoke(this, new GenericSingleEventArgs<string>(ex.GetType().Name + ": " + ex.Message));
                    return;
                }

                if (n <= 0)
                {
                    ConnectionFailed?.Invoke(this, new GenericSingleEventArgs<string>("Remote closed the connection."));
                    return;
                }

                var slice = new ReadOnlyMemory<byte>(buffer, 0, n);
                RxReceived?.Invoke(this, new GenericSingleEventArgs<ReadOnlyMemory<byte>>(slice));
            }
        }
        catch (Exception ex)
        {
            ConnectionFailed?.Invoke(this, new GenericSingleEventArgs<string>("Read loop terminated unexpectedly: " + ex.Message));
        }
    }
}
