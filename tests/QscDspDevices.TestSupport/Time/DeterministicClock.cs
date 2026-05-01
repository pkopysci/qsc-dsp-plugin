// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QscDspDevices.Plugin.Threading;

namespace QscDspDevices.TestSupport.Time;

/// <summary>
/// Manually-advanced <see cref="IQrcClock"/> for deterministic timing in
/// tests. Time only advances when the test calls <see cref="Advance"/>;
/// any pending <see cref="DelayAsync"/> whose deadline is reached
/// completes immediately.
/// </summary>
/// <remarks>
/// <para>
/// Backed by a sorted list of pending waiters keyed by deadline. The
/// implementation is deliberately simple: tests advance time in chunks
/// and assert on observable side effects. Concurrent advancement from
/// multiple threads is supported via a single internal lock.
/// </para>
/// <para>
/// Production code SHOULD NEVER reference this type. Reference
/// <see cref="IQrcClock"/> only and inject the implementation at the
/// composition root.
/// </para>
/// </remarks>
public sealed class DeterministicClock : IQrcClock
{
    private readonly object _lock = new();
    private readonly List<Waiter> _waiters = new();
    private TaskCompletionSource _nextWaiterAdded = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private DateTime _now;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicClock"/> class
    /// at the supplied virtual start time.
    /// </summary>
    /// <param name="start">The initial virtual time. Defaults to UTC epoch.</param>
    public DeterministicClock(DateTime? start = null)
    {
        _now = start ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    /// <inheritdoc />
    public DateTime UtcNow
    {
        get
        {
            lock (_lock)
            {
                return _now;
            }
        }
    }

    /// <summary>
    /// Gets the count of currently-pending waiters whose deadlines are
    /// in the future. Test diagnostics; not used by production code.
    /// </summary>
    public int PendingWaiters
    {
        get
        {
            lock (_lock)
            {
                return _waiters.Count;
            }
        }
    }

    /// <summary>
    /// Returns a Task that completes the next time a waiter is added via
    /// <see cref="DelayAsync"/>. Tests use this to gate <see cref="Advance"/>
    /// on the production code actually reaching its delay call — polling
    /// on <see cref="PendingWaiters"/> is racy on slow CI runners. Each
    /// call returns the same Task for the in-flight wait; once it fires,
    /// a fresh Task is created for the next waiter.
    /// </summary>
    /// <returns>A Task that completes when the next waiter registers.</returns>
    public Task WhenNextWaiterAddedAsync()
    {
        lock (_lock)
        {
            return _nextWaiterAdded.Task;
        }
    }

    /// <inheritdoc />
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "Delay must be non-negative.");
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Waiter waiter;
        TaskCompletionSource notify;
        lock (_lock)
        {
            waiter = new Waiter(_now + delay, tcs);
            _waiters.Add(waiter);
            notify = _nextWaiterAdded;
            _nextWaiterAdded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        notify.TrySetResult();

        cancellationToken.Register(() =>
        {
            lock (_lock)
            {
                _waiters.Remove(waiter);
            }

            tcs.TrySetCanceled(cancellationToken);
        });

        // If the supplied delay was zero, complete synchronously.
        if (delay == TimeSpan.Zero)
        {
            lock (_lock)
            {
                _waiters.Remove(waiter);
            }

            tcs.TrySetResult();
        }

        return tcs.Task;
    }

    /// <summary>
    /// Advances virtual time by <paramref name="delta"/> and completes
    /// every waiter whose deadline is at or before the new time.
    /// </summary>
    /// <param name="delta">The amount of virtual time to advance.</param>
    /// <returns>The number of waiters released by this advance.</returns>
    public int Advance(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Cannot rewind virtual time.");
        }

        List<Waiter> released;
        lock (_lock)
        {
            _now += delta;

            released = new List<Waiter>();
            for (int i = _waiters.Count - 1; i >= 0; i--)
            {
                if (_waiters[i].Deadline <= _now)
                {
                    released.Add(_waiters[i]);
                    _waiters.RemoveAt(i);
                }
            }
        }

        foreach (Waiter w in released)
        {
            w.Source.TrySetResult();
        }

        return released.Count;
    }

    private sealed record Waiter(DateTime Deadline, TaskCompletionSource Source);
}
