// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.TestSupport.Time;
using Xunit;

namespace QscDspDevices.UnitTests.Plugin;

/// <summary>
/// Unit tests for the test-only <see cref="DeterministicClock"/>. Even
/// though it lives in TestSupport, its correctness underlies every
/// timing-sensitive test in M2 onwards, so we test it directly.
/// </summary>
public sealed class DeterministicClockTests
{
    [Fact]
    public async Task DelayAsync_completes_after_Advance_passes_the_deadline()
    {
        var clock = new DeterministicClock();
        Task task = clock.DelayAsync(TimeSpan.FromSeconds(15), CancellationToken.None);

        task.IsCompleted.Should().BeFalse();
        clock.PendingWaiters.Should().Be(1);

        clock.Advance(TimeSpan.FromSeconds(14));
        task.IsCompleted.Should().BeFalse();

        clock.Advance(TimeSpan.FromSeconds(1));

        await task;
        clock.PendingWaiters.Should().Be(0);
    }

    [Fact]
    public async Task DelayAsync_with_zero_delay_completes_synchronously()
    {
        var clock = new DeterministicClock();
        await clock.DelayAsync(TimeSpan.Zero, CancellationToken.None);
        clock.PendingWaiters.Should().Be(0);
    }

    [Fact]
    public async Task Multiple_waiters_release_in_deadline_order()
    {
        var clock = new DeterministicClock();
        Task early = clock.DelayAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        Task late = clock.DelayAsync(TimeSpan.FromSeconds(15), CancellationToken.None);

        clock.Advance(TimeSpan.FromSeconds(5));
        await early;
        late.IsCompleted.Should().BeFalse();

        clock.Advance(TimeSpan.FromSeconds(10));
        await late;
    }

    [Fact]
    public async Task Cancellation_aborts_pending_DelayAsync()
    {
        var clock = new DeterministicClock();
        using var cts = new CancellationTokenSource();
        Task task = clock.DelayAsync(TimeSpan.FromMinutes(1), cts.Token);

        await cts.CancelAsync();

        await FluentActions.Awaiting(async () => await task).Should().ThrowAsync<OperationCanceledException>();
        clock.PendingWaiters.Should().Be(0);
    }

    [Fact]
    public void Negative_delay_throws()
    {
        var clock = new DeterministicClock();
        Action act = () => clock.DelayAsync(TimeSpan.FromSeconds(-1), CancellationToken.None);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Advance_with_negative_delta_throws()
    {
        var clock = new DeterministicClock();
        Action act = () => clock.Advance(TimeSpan.FromSeconds(-1));
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UtcNow_advances_with_Advance()
    {
        var clock = new DeterministicClock(new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc));
        DateTime initial = clock.UtcNow;

        clock.Advance(TimeSpan.FromHours(1));

        clock.UtcNow.Should().Be(initial.AddHours(1));
    }

    [Fact]
    public async Task WhenNextWaiterAddedAsync_completes_when_a_waiter_registers()
    {
        var clock = new DeterministicClock();
        Task notify = clock.WhenNextWaiterAddedAsync();

        notify.IsCompleted.Should().BeFalse();

        Task waiter = clock.DelayAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

        await notify.WaitAsync(TimeSpan.FromSeconds(2));
        notify.IsCompleted.Should().BeTrue();
        waiter.IsCompleted.Should().BeFalse();

        clock.Advance(TimeSpan.FromSeconds(5));
        await waiter;
    }

    [Fact]
    public async Task WhenNextWaiterAddedAsync_returns_a_fresh_task_after_each_waiter()
    {
        var clock = new DeterministicClock();

        Task first = clock.WhenNextWaiterAddedAsync();
        _ = clock.DelayAsync(TimeSpan.FromSeconds(1), CancellationToken.None);
        await first.WaitAsync(TimeSpan.FromSeconds(2));

        Task second = clock.WhenNextWaiterAddedAsync();
        second.IsCompleted.Should().BeFalse();

        _ = clock.DelayAsync(TimeSpan.FromSeconds(1), CancellationToken.None);
        await second.WaitAsync(TimeSpan.FromSeconds(2));
    }
}
