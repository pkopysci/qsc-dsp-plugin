// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.Connectivity.Ecp;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity.Ecp;

public sealed class EcpRoutingCommandQueueTests
{
    [Fact]
    public void TryEnqueue_with_no_active_returns_false()
    {
        using var sut = new EcpRoutingCommandQueue("dsp-1");
        sut.TryEnqueue("sg").Should().BeFalse();
    }

    [Fact]
    public void SetActive_then_TryEnqueue_forwards_to_inner()
    {
        using var inner = new EcpCommandQueue("dsp-1");
        inner.StartAccepting();
        using var sut = new EcpRoutingCommandQueue("dsp-1");
        sut.SetActive(inner);

        sut.TryEnqueue("sg").Should().BeTrue();
        inner.SnapshotPending().Should().Equal("sg");
    }

    [Fact]
    public void SetActive_null_clears_routing()
    {
        using var inner = new EcpCommandQueue("dsp-1");
        inner.StartAccepting();
        using var sut = new EcpRoutingCommandQueue("dsp-1");
        sut.SetActive(inner);
        sut.SetActive(null);

        sut.TryEnqueue("sg").Should().BeFalse();
    }

    [Fact]
    public void SetActive_with_same_queue_is_idempotent()
    {
        using var inner = new EcpCommandQueue("dsp-1");
        using var sut = new EcpRoutingCommandQueue("dsp-1");
        sut.SetActive(inner);
        Action act = () => sut.SetActive(inner);
        act.Should().NotThrow();
    }

    [Fact]
    public void TryEnqueue_throws_on_null()
    {
        using var sut = new EcpRoutingCommandQueue("dsp-1");
        ((Action)(() => sut.TryEnqueue(null!))).Should().Throw<ArgumentNullException>();
    }
}
