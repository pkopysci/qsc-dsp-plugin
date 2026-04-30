// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.Protocol;

/// <summary>
/// FIFO queue for outbound JSON-RPC requests. Refuses to enqueue while
/// the connection is anything other than Connected (per README
/// §"Sending/Receiving") and is atomically drained on every disconnect
/// transition.
/// </summary>
/// <remarks>
/// <para>
/// Backed by <see cref="System.Threading.Channels.Channel{T}"/>. The
/// connection manager owns this queue and toggles
/// <see cref="StartAccepting"/> / <see cref="Drain"/> as the state
/// machine transitions.
/// </para>
/// <para>
/// Bound: 1024 outstanding requests by default. When saturated, the
/// oldest entry is dropped and a <c>Logger.Warn</c> is emitted —
/// mirrors the design.md decision that "newer commands should win when
/// the device can't keep up".
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "The README, OpenSpec proposal, and qsc-critic checklist all refer to this object as the 'command queue'. Renaming to e.g. CommandPipeline would diverge from the spec language and regress audit-friendliness.")]
public sealed class CommandQueue : IDisposable
{
    /// <summary>The default upper bound on outstanding requests.</summary>
    public const int DefaultCapacity = 1024;

    private readonly string _deviceId;
    private readonly Channel<JsonRpcRequest> _channel;
    private readonly object _stateLock = new();
    private bool _accepting;
    private bool _disposed;
    private long _droppedTotal;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandQueue"/> class
    /// with the specified id used in log messages and the default capacity.
    /// </summary>
    /// <param name="deviceId">The owning device id (used in log messages).</param>
    public CommandQueue(string deviceId)
        : this(deviceId, DefaultCapacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandQueue"/> class
    /// with explicit capacity.
    /// </summary>
    /// <param name="deviceId">The owning device id (used in log messages).</param>
    /// <param name="capacity">Maximum outstanding requests; oldest dropped on saturation.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="deviceId"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity"/> is non-positive.</exception>
    public CommandQueue(string deviceId, int capacity)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
        }

        _deviceId = deviceId;
        _channel = Channel.CreateBounded<JsonRpcRequest>(new BoundedChannelOptions(capacity)
        {
            // We implement oldest-drop semantics manually so we can log
            // a Logger.Warn on every drop. Channel's built-in DropOldest
            // is silent; we want the noise.
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true,
        });

        Capacity = capacity;
    }

    /// <summary>Gets the configured maximum outstanding-request count.</summary>
    public int Capacity { get; }

    /// <summary>
    /// Gets a value indicating whether the queue currently accepts enqueues.
    /// </summary>
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

    /// <summary>Gets the cumulative count of saturation-driven drops since construction.</summary>
    public long DroppedTotal => Interlocked.Read(ref _droppedTotal);

    /// <summary>
    /// Allows enqueues. Called by the connection manager on the transition
    /// into Connected.
    /// </summary>
    public void StartAccepting()
    {
        lock (_stateLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _accepting = true;
        }
    }

    /// <summary>
    /// Atomically refuses further enqueues and discards every queued
    /// request. Called by the connection manager on every transition
    /// into Disconnected. The number of discarded items is logged at
    /// <c>Logger.Notice</c>.
    /// </summary>
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
            Log.Notice(_deviceId, $"CommandQueue drained {discarded} pending request(s) on disconnect.");
        }
    }

    /// <summary>
    /// Attempts to enqueue a request. While the queue is not accepting,
    /// returns <c>false</c> and logs <c>Logger.Error</c> "command attempted
    /// while disconnected". When the queue is full, the oldest entry is
    /// removed, the new entry is enqueued, and a <c>Logger.Warn</c>
    /// "queue saturated" is logged.
    /// </summary>
    /// <param name="request">The request to enqueue.</param>
    /// <returns><c>true</c> if the request was enqueued; <c>false</c> if
    /// the queue refused (disconnected or disposed).</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="request"/> is null.</exception>
    public bool TryEnqueue(JsonRpcRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_stateLock)
        {
            if (_disposed || !_accepting)
            {
                Log.Error(_deviceId, $"Command attempted while disconnected: method={request.Method} id={request.Id}");
                return false;
            }
        }

        // Fast path: the channel has room.
        if (_channel.Writer.TryWrite(request))
        {
            return true;
        }

        // Slow path: saturated. Drop the oldest, enqueue the new one,
        // and log a Warn. We do this under the state lock so the drop +
        // enqueue is observed atomically by Drain() callers.
        lock (_stateLock)
        {
            if (_disposed || !_accepting)
            {
                Log.Error(_deviceId, $"Command attempted while disconnected: method={request.Method} id={request.Id}");
                return false;
            }

            if (!_channel.Reader.TryRead(out _))
            {
                // Race: the reader emptied us between our TryWrite and the
                // saturation drop. Try one more write; if it still fails,
                // bail out.
                if (_channel.Writer.TryWrite(request))
                {
                    return true;
                }

                Log.Warn(_deviceId, $"Command queue saturated; could not enqueue method={request.Method} id={request.Id}");
                return false;
            }

            Interlocked.Increment(ref _droppedTotal);

            if (_channel.Writer.TryWrite(request))
            {
                Log.Warn(_deviceId, $"Command queue saturated; oldest command dropped to make room for method={request.Method} id={request.Id}");
                return true;
            }

            Log.Warn(_deviceId, $"Command queue saturated and unable to enqueue even after dropping oldest; method={request.Method} id={request.Id}");
            return false;
        }
    }

    /// <summary>
    /// Asynchronously dequeues the next request, awaiting until one
    /// arrives or the supplied <paramref name="cancellationToken"/> is
    /// cancelled. Used by the send-loop.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The next request.</returns>
    /// <exception cref="OperationCanceledException">If
    /// <paramref name="cancellationToken"/> is cancelled.</exception>
    public ValueTask<JsonRpcRequest> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);

    /// <summary>
    /// Drains the queue synchronously, returning every currently-buffered
    /// request without blocking. Intended for tests.
    /// </summary>
    /// <returns>The remaining requests in FIFO order.</returns>
    public IReadOnlyList<JsonRpcRequest> SnapshotPending()
    {
        var result = new List<JsonRpcRequest>();
        while (_channel.Reader.TryRead(out var item))
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
}
