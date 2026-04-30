// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.Connectivity;
using QscDspDevices.TestSupport.Time;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity;

/// <summary>
/// Unit tests for <see cref="ReconnectStrategy"/>. The README mandates a
/// fixed 15-second wait between attempts. Verified deterministically via
/// <see cref="DeterministicClock"/>.
/// </summary>
public sealed class ReconnectStrategyTests
{
    [Fact]
    public void Interval_is_exactly_fifteen_seconds()
    {
        ReconnectStrategy.Interval.Should().Be(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public async Task WaitForNextAttemptAsync_completes_after_fifteen_seconds()
    {
        var clock = new DeterministicClock();
        var sut = new ReconnectStrategy(clock);

        var task = sut.WaitForNextAttemptAsync(CancellationToken.None);
        task.IsCompleted.Should().BeFalse();

        clock.Advance(TimeSpan.FromSeconds(14));
        task.IsCompleted.Should().BeFalse();

        clock.Advance(TimeSpan.FromSeconds(1));
        await task;
    }

    [Fact]
    public async Task Cancellation_aborts_the_wait()
    {
        var clock = new DeterministicClock();
        var sut = new ReconnectStrategy(clock);
        using var cts = new CancellationTokenSource();

        var task = sut.WaitForNextAttemptAsync(cts.Token);
        await cts.CancelAsync();

        await FluentActions.Awaiting(async () => await task)
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Constructor_with_null_clock_throws()
    {
        Action act = () => _ = new ReconnectStrategy(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
