// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

namespace QscDspDevices.Plugin.Threading;

/// <summary>
/// Production <see cref="IQrcClock"/> backed by <see cref="DateTime.UtcNow"/>
/// and <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.
/// </summary>
/// <remarks>
/// This is the only place in the production codebase that may reference
/// <c>DateTime.UtcNow</c> or <c>Task.Delay</c>. Every other component
/// receives an <see cref="IQrcClock"/> by constructor injection so that
/// timing-sensitive behaviour is exercised deterministically in tests.
/// </remarks>
public sealed class SystemClock : IQrcClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc />
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delay),
                delay,
                "Delay must be non-negative.");
        }

        return Task.Delay(delay, cancellationToken);
    }
}
