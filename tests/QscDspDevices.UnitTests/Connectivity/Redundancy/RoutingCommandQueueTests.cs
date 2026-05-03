// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.Connectivity.Redundancy;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.JsonRpc;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity.Redundancy;

/// <summary>
/// Unit tests for <see cref="RoutingCommandQueue"/>.
/// </summary>
public sealed class RoutingCommandQueueTests
{
    [Fact]
    public void TryEnqueue_with_no_active_returns_false()
    {
        using var sut = new RoutingCommandQueue("dsp-1");
        var request = new JsonRpcRequest { Id = 1, Method = "Control.Set" };

        sut.TryEnqueue(request).Should().BeFalse();
    }

    [Fact]
    public void TryEnqueue_after_SetActive_forwards_to_the_underlying_queue()
    {
        using var underlying = new CommandQueue("dsp-1");
        underlying.StartAccepting();
        using var sut = new RoutingCommandQueue("dsp-1");
        sut.SetActive(underlying);

        var request = new JsonRpcRequest { Id = 1, Method = "Control.Set" };
        sut.TryEnqueue(request).Should().BeTrue();

        underlying.SnapshotPending().Should().HaveCount(1);
    }

    [Fact]
    public void SetActive_swap_routes_to_the_new_queue()
    {
        using var first = new CommandQueue("dsp-1");
        first.StartAccepting();
        using var second = new CommandQueue("dsp-1");
        second.StartAccepting();
        using var sut = new RoutingCommandQueue("dsp-1");

        sut.SetActive(first);
        sut.TryEnqueue(new JsonRpcRequest { Id = 1, Method = "A" });

        sut.SetActive(second);
        sut.TryEnqueue(new JsonRpcRequest { Id = 2, Method = "B" });

        first.SnapshotPending().Should().HaveCount(1);
        second.SnapshotPending().Should().HaveCount(1);
    }

    [Fact]
    public void SetActive_null_after_an_active_clears_routing_and_refuses_subsequent_writes()
    {
        using var underlying = new CommandQueue("dsp-1");
        underlying.StartAccepting();
        using var sut = new RoutingCommandQueue("dsp-1");
        sut.SetActive(underlying);
        sut.TryEnqueue(new JsonRpcRequest { Id = 1, Method = "A" }).Should().BeTrue();

        sut.SetActive(null);
        sut.TryEnqueue(new JsonRpcRequest { Id = 2, Method = "B" }).Should().BeFalse();
    }

    [Fact]
    public void SetActive_with_the_same_queue_twice_is_idempotent()
    {
        using var underlying = new CommandQueue("dsp-1");
        underlying.StartAccepting();
        using var sut = new RoutingCommandQueue("dsp-1");

        Action act = () =>
        {
            sut.SetActive(underlying);
            sut.SetActive(underlying);
        };
        act.Should().NotThrow();
    }

    [Fact]
    public void TryEnqueue_with_null_throws()
    {
        using var sut = new RoutingCommandQueue("dsp-1");
        Action act = () => sut.TryEnqueue(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_with_null_deviceId_throws()
    {
        Action act = () => _ = new RoutingCommandQueue(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
