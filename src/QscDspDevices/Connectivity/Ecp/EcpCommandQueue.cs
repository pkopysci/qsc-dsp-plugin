// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using QscDspDevices.Plugin;

namespace QscDspDevices.Connectivity.Ecp;

/// <summary>
/// FIFO queue for outbound ECP command lines. Mirrors the M2 QRC
/// <see cref="QscDspDevices.Protocol.CommandQueue"/> contract: refuses
/// to enqueue while not Accepting, drains atomically on disconnect,
/// drops oldest on saturation with a <c>Logger.Warn</c>.
/// </summary>
/// <remarks>
/// Per design.md §D-E1, the M-ECP backend ships a parallel queue rather
/// than widening the M3-M5 service abstraction. Commands are bare
/// strings (the wire text from <see cref="QscDspDevices.Protocol.Ecp.EcpCommand"/>);
/// the framer appends the LF terminator before they hit the transport.
/// </remarks>
internal sealed class EcpCommandQueue : IDisposable
{
    /// <summary>The default upper bound on outstanding commands.</summary>
    public const int DefaultCapacity = 1024;

    private readonly string _deviceId;
    private readonly Channel<string> _channel;
    private readonly object _stateLock = new();
    private bool _accepting;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpCommandQueue"/> class
    /// with the default capacity.
    /// </summary>
    /// <param name="deviceId">The owning device id (for log messages).</param>
    /// <exception cref="ArgumentNullException">If <paramref name="deviceId"/> is null.</exception>
    public EcpCommandQueue(string deviceId)
        : this(deviceId, DefaultCapacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpCommandQueue"/> class
    /// with explicit capacity.
    /// </summary>
    /// <param name="deviceId">The owning device id (for log messages).</param>
    /// <param name="capacity">Maximum outstanding commands; oldest dropped on saturation.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="deviceId"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity"/> is non-positive.</exception>
    public EcpCommandQueue(string deviceId, int capacity)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
        }

        _deviceId = deviceId;
        _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true,
        });
        Capacity = capacity;
    }

    /// <summary>Gets the configured maximum outstanding-command count.</summary>
    public int Capacity
    {
        get;
    }

    /// <summary>Gets a value indicating whether the queue currently accepts enqueues.</summary>
    public bool IsAccepting
    {
        get
        {
            lock (_stateLock)
            {
                return _accepting;
            }
        }
    }

    /// <summary>Allows enqueues. Called by the connection manager on Connected.</summary>
    public void StartAccepting()
    {
        lock (_stateLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _accepting = true;
        }
    }

    /// <summary>Atomically refuses further enqueues and discards every queued command.</summary>
    public void Drain()
    {
        lock (_stateLock)
        {
            if (_disposed)
            {
                return;
            }

            _accepting = false;
        }

        int discarded = 0;
        while (_channel.Reader.TryRead(out _))
        {
            discarded++;
        }

        if (discarded > 0)
        {
            Log.Notice(_deviceId, $"EcpCommandQueue drained {discarded} pending command(s) on disconnect.");
        }
    }

    /// <summary>
    /// Attempts to enqueue a command. Refuses (returns false + logs
    /// Error) when not accepting; drops oldest + logs Warn on saturation.
    /// </summary>
    /// <param name="command">The command wire text.</param>
    /// <returns>True if enqueued.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="command"/> is null.</exception>
    public bool TryEnqueue(string command)
    {
        ArgumentNullException.ThrowIfNull(command);

        lock (_stateLock)
        {
            if (_disposed)
            {
                Log.Error(_deviceId, $"ECP command attempted on disposed queue: '{Truncate(command)}'");
                return false;
            }

            if (!_accepting)
            {
                Log.Error(_deviceId, $"ECP command attempted while disconnected: '{Truncate(command)}'");
                return false;
            }

            if (_channel.Writer.TryWrite(command))
            {
                return true;
            }

            if (_channel.Reader.TryRead(out _) && _channel.Writer.TryWrite(command))
            {
                Log.Warn(_deviceId, $"ECP queue saturated; oldest command dropped to make room for '{Truncate(command)}'.");
                return true;
            }

            Log.Warn(_deviceId, $"ECP queue saturated and unable to enqueue '{Truncate(command)}'.");
            return false;
        }
    }

    /// <summary>Asynchronously dequeues the next command.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The next command.</returns>
    public ValueTask<string> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);

    /// <summary>Drains the queue synchronously. Test helper.</summary>
    /// <returns>The remaining commands in FIFO order.</returns>
    public IReadOnlyList<string> SnapshotPending()
    {
        var result = new List<string>();
        while (_channel.Reader.TryRead(out string? item))
        {
            result.Add(item);
        }

        return result;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_stateLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _accepting = false;
        }

        _channel.Writer.TryComplete();
    }

    private static string Truncate(string s) => s.Length <= 100 ? s : s[..100] + "...";
}
