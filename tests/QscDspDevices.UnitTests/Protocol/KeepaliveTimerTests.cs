// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.JsonRpc;
using QscDspDevices.TestSupport.Time;
using Xunit;

namespace QscDspDevices.UnitTests.Protocol;

/// <summary>
/// Unit tests for <see cref="KeepaliveTimer"/>. The timer's behaviour is
/// purely a function of the clock and the most-recent outbound timestamp,
/// so DeterministicClock makes these tests entirely synchronous.
/// </summary>
public sealed class KeepaliveTimerTests
{
    [Fact]
    public async Task Tick_before_interval_does_not_send()
    {
        var clock = new DeterministicClock();
        var sent = new List<JsonRpcRequest>();
        var sut = new KeepaliveTimer(clock, new IdGenerator(), AppendTo(sent, accept: true));

        // Move just under the interval.
        clock.Advance(TimeSpan.FromSeconds(29));
        await sut.TickAsync(CancellationToken.None);

        sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Tick_at_or_after_interval_sends_a_NoOp()
    {
        var clock = new DeterministicClock();
        var sent = new List<JsonRpcRequest>();
        var sut = new KeepaliveTimer(clock, new IdGenerator(), AppendTo(sent, accept: true));

        clock.Advance(TimeSpan.FromSeconds(30));
        await sut.TickAsync(CancellationToken.None);

        sent.Should().HaveCount(1);
        sent[0].Method.Should().Be("NoOp");
        sent[0].Id.Should().Be(1L);
    }

    [Fact]
    public async Task Outbound_activity_resets_the_silence_window()
    {
        var clock = new DeterministicClock();
        var sent = new List<JsonRpcRequest>();
        var sut = new KeepaliveTimer(clock, new IdGenerator(), AppendTo(sent, accept: true));

        // Send-loop wrote at t=20.
        clock.Advance(TimeSpan.FromSeconds(20));
        sut.NotifyOutboundSent();

        // At t=45 (25s after most recent outbound) the timer should NOT fire.
        clock.Advance(TimeSpan.FromSeconds(25));
        await sut.TickAsync(CancellationToken.None);
        sent.Should().BeEmpty();

        // At t=51 (31s after most recent outbound) the timer SHOULD fire.
        clock.Advance(TimeSpan.FromSeconds(6));
        await sut.TickAsync(CancellationToken.None);
        sent.Should().HaveCount(1);
    }

    [Fact]
    public async Task Send_callback_returning_false_still_returns_normally()
    {
        var clock = new DeterministicClock();
        var sent = new List<JsonRpcRequest>();
        var sut = new KeepaliveTimer(clock, new IdGenerator(), AppendTo(sent, accept: false));

        clock.Advance(TimeSpan.FromSeconds(30));
        await sut.TickAsync(CancellationToken.None);

        sent.Should().HaveCount(1);
    }

    [Fact]
    public void Constructor_rejects_null_args()
    {
        var clock = new DeterministicClock();
        var idGen = new IdGenerator();
        Func<JsonRpcRequest, ValueTask<bool>> send = _ => ValueTask.FromResult(true);

        Action nullClock = () => _ = new KeepaliveTimer(null!, idGen, send);
        Action nullIdGen = () => _ = new KeepaliveTimer(clock, null!, send);
        Action nullSend = () => _ = new KeepaliveTimer(clock, idGen, null!);

        nullClock.Should().Throw<ArgumentNullException>();
        nullIdGen.Should().Throw<ArgumentNullException>();
        nullSend.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_with_non_positive_interval_throws()
    {
        var clock = new DeterministicClock();
        var idGen = new IdGenerator();
        Action act = () => _ = new KeepaliveTimer(
            clock,
            idGen,
            _ => ValueTask.FromResult(true),
            TimeSpan.Zero);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static Func<JsonRpcRequest, ValueTask<bool>> AppendTo(List<JsonRpcRequest> sink, bool accept)
        => request =>
        {
            sink.Add(request);
            return ValueTask.FromResult(accept);
        };
}
