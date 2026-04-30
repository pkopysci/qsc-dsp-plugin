// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

namespace QscDspDevices.Plugin.Threading;

/// <summary>
/// Abstraction over wall-clock time and asynchronous waits used by every
/// time-aware component in the plugin (keepalive timer, reconnect strategy,
/// etc.). Production code wires <see cref="SystemClock"/>; tests wire
/// <c>DeterministicClock</c> from the TestSupport project.
/// </summary>
/// <remarks>
/// No production code SHALL call <c>DateTime.Now</c>, <c>DateTime.UtcNow</c>,
/// <c>Thread.Sleep</c>, or <c>Task.Delay</c> directly — they MUST go through
/// this abstraction so the 15-second reconnect interval and the 30-second
/// keepalive can be exercised deterministically by tests instead of by
/// real wall-clock waits.
/// </remarks>
public interface IQrcClock
{
    /// <summary>
    /// Gets the current UTC time according to the clock implementation.
    /// </summary>
    /// <returns>The current UTC time.</returns>
    DateTime UtcNow { get; }

    /// <summary>
    /// Asynchronously waits for the specified duration before completing.
    /// </summary>
    /// <param name="delay">How long to wait. Must be non-negative.</param>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    /// <returns>A task that completes after <paramref name="delay"/> elapses
    /// or when <paramref name="cancellationToken"/> is cancelled.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="delay"/>
    /// is negative.</exception>
    /// <exception cref="OperationCanceledException">If the wait is cancelled.</exception>
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
