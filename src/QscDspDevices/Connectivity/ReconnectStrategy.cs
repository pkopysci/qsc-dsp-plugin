// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using QscDspDevices.Plugin.Threading;

namespace QscDspDevices.Connectivity;

/// <summary>
/// Implements the README §"Device Connection" reconnect policy: wait
/// exactly 15 seconds between attempts, repeat until either the caller
/// cancels (i.e. <c>Disconnect()</c> was called) or a connection
/// succeeds.
/// </summary>
/// <remarks>
/// The interval is deliberately constant — the README is specific
/// ("wait 15 seconds, and attempt to reconnect. This will be repeated
/// until BaseDevice.Disconnect() is called externally or a connection
/// is established."). Exponential backoff would be a defect.
/// </remarks>
public sealed class ReconnectStrategy
{
    /// <summary>The fixed wait between reconnect attempts, per README.</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    private readonly IQrcClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconnectStrategy"/> class.
    /// </summary>
    /// <param name="clock">The clock used to wait between attempts.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="clock"/> is null.</exception>
    public ReconnectStrategy(IQrcClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    /// <summary>
    /// Asynchronously waits the configured interval, then returns. The
    /// caller (the connection manager) drives the actual connect attempt
    /// after this returns.
    /// </summary>
    /// <param name="cancellationToken">Cancellation aborts the wait.</param>
    /// <returns>A task that completes once the wait elapses.</returns>
    public Task WaitForNextAttemptAsync(CancellationToken cancellationToken)
        => _clock.DelayAsync(Interval, cancellationToken);
}
