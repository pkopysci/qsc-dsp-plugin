// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.Plugin.Threading;
using Xunit;

namespace QscDspDevices.UnitTests.Plugin;

/// <summary>
/// Unit tests for the production <see cref="SystemClock"/>. Kept brief —
/// this is the only place in the codebase that legitimately uses
/// <c>DateTime.UtcNow</c> and <c>Task.Delay</c>; we just confirm both
/// behave like the platform contract.
/// </summary>
public sealed class SystemClockTests
{
    [Fact]
    public void UtcNow_returns_a_recent_DateTime()
    {
        var sut = new SystemClock();
        DateTime now = sut.UtcNow;
        DateTime reference = DateTime.UtcNow;

        // The two calls were made within milliseconds of each other.
        (reference - now).Duration().Should().BeLessThan(TimeSpan.FromSeconds(1));
        now.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task DelayAsync_with_short_delay_completes()
    {
        var sut = new SystemClock();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await sut.DelayAsync(TimeSpan.FromMilliseconds(20), CancellationToken.None);
        stopwatch.Stop();
        stopwatch.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(15));
    }

    [Fact]
    public void DelayAsync_with_negative_delay_throws()
    {
        var sut = new SystemClock();
        Action act = () => sut.DelayAsync(TimeSpan.FromSeconds(-1), CancellationToken.None);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task DelayAsync_respects_cancellation()
    {
        var sut = new SystemClock();
        using var cts = new CancellationTokenSource();
        Task task = sut.DelayAsync(TimeSpan.FromSeconds(60), cts.Token);

        await cts.CancelAsync();

        await FluentActions.Awaiting(async () => await task)
            .Should().ThrowAsync<OperationCanceledException>();
    }
}
