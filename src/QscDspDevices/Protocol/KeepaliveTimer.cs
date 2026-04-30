// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.Protocol;

/// <summary>
/// Emits a <c>NoOp</c> JSON-RPC request after every period of outbound
/// silence longer than <see cref="Interval"/>. The default 30-second
/// interval sits comfortably inside QSC's 60-second silence-disconnect
/// window (per <c>research/QRC_PROTOCOL.md §3</c>).
/// </summary>
/// <remarks>
/// <para>
/// The send-loop is responsible for calling <see cref="NotifyOutboundSent"/>
/// every time it writes a frame. The keepalive timer subtracts that
/// timestamp from the current clock to decide whether enough silence has
/// elapsed to warrant a NoOp.
/// </para>
/// <para>
/// The timer does not own a thread of its own — it is driven by the
/// shared timer thread inside <c>PluginTimer</c>. M2's connection
/// manager passes the keepalive into the timer-loop's tick.
/// </para>
/// </remarks>
public sealed class KeepaliveTimer
{
    /// <summary>The default outbound-silence interval before a NoOp is emitted.</summary>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(30);

    private readonly IQrcClock _clock;
    private readonly IdGenerator _idGenerator;
    private readonly Func<JsonRpcRequest, ValueTask<bool>> _sendAsync;
    private readonly object _lock = new();
    private DateTime _lastOutbound;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeepaliveTimer"/> class.
    /// </summary>
    /// <param name="clock">The clock to read the current UTC time from.</param>
    /// <param name="idGenerator">Source of monotonic ids for the NoOp request.</param>
    /// <param name="sendAsync">Callback invoked to enqueue the NoOp request. The callback returns <c>true</c> when the send was accepted by the queue.</param>
    /// <param name="interval">Optional override for the silence interval.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public KeepaliveTimer(
        IQrcClock clock,
        IdGenerator idGenerator,
        Func<JsonRpcRequest, ValueTask<bool>> sendAsync,
        TimeSpan? interval = null)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(idGenerator);
        ArgumentNullException.ThrowIfNull(sendAsync);

        _clock = clock;
        _idGenerator = idGenerator;
        _sendAsync = sendAsync;
        Interval = interval ?? DefaultInterval;

        if (Interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), Interval, "Interval must be positive.");
        }

        _lastOutbound = _clock.UtcNow;
    }

    /// <summary>Gets the configured silence interval.</summary>
    public TimeSpan Interval
    {
        get;
    }

    /// <summary>
    /// Notifies the timer that an outbound frame was just sent. Resets
    /// the silence countdown. Called by the send-loop on every write.
    /// </summary>
    public void NotifyOutboundSent()
    {
        lock (_lock)
        {
            _lastOutbound = _clock.UtcNow;
        }
    }

    /// <summary>
    /// Asks the timer whether a NoOp is overdue and, if so, sends one
    /// through the supplied <c>sendAsync</c> callback. Called by the
    /// timer thread on each tick.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes after a tick is processed.</returns>
    public async ValueTask TickAsync(CancellationToken cancellationToken)
    {
        TimeSpan elapsed;
        lock (_lock)
        {
            elapsed = _clock.UtcNow - _lastOutbound;
        }

        if (elapsed < Interval)
        {
            return;
        }

        var request = new JsonRpcRequest
        {
            Id = _idGenerator.Next(),
            Method = "NoOp",
        };

        bool accepted = await _sendAsync(request).ConfigureAwait(false);
        if (accepted)
        {
            // The send-loop will call NotifyOutboundSent when it actually
            // dequeues and writes; until then we optimistically reset so
            // we don't enqueue ten NoOps while waiting for the first to
            // be drained.
            NotifyOutboundSent();
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
