// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.Connectivity.Ecp;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity.Ecp;

public sealed class EcpCommandQueueTests
{
    [Fact]
    public void New_queue_does_not_accept_until_StartAccepting()
    {
        using var queue = new EcpCommandQueue("dsp-1");
        queue.IsAccepting.Should().BeFalse();
        queue.TryEnqueue("sg").Should().BeFalse();
    }

    [Fact]
    public void StartAccepting_then_TryEnqueue_succeeds()
    {
        using var queue = new EcpCommandQueue("dsp-1");
        queue.StartAccepting();
        queue.TryEnqueue("sg").Should().BeTrue();
        queue.SnapshotPending().Should().Equal("sg");
    }

    [Fact]
    public void Drain_clears_pending_and_blocks_further_enqueue()
    {
        using var queue = new EcpCommandQueue("dsp-1");
        queue.StartAccepting();
        queue.TryEnqueue("sg");
        queue.TryEnqueue("ct \"play\"");

        queue.Drain();

        queue.IsAccepting.Should().BeFalse();
        queue.SnapshotPending().Should().BeEmpty();
        queue.TryEnqueue("cgpa").Should().BeFalse();
    }

    [Fact]
    public void Saturated_queue_drops_oldest()
    {
        using var queue = new EcpCommandQueue("dsp-1", capacity: 2);
        queue.StartAccepting();
        queue.TryEnqueue("a").Should().BeTrue();
        queue.TryEnqueue("b").Should().BeTrue();
        queue.TryEnqueue("c").Should().BeTrue();

        queue.SnapshotPending().Should().Equal("b", "c");
    }

    [Fact]
    public void Disposed_queue_refuses()
    {
        var queue = new EcpCommandQueue("dsp-1");
        queue.Dispose();
        queue.TryEnqueue("sg").Should().BeFalse();
    }

    [Fact]
    public void Dispose_is_idempotent()
    {
        var queue = new EcpCommandQueue("dsp-1");
        queue.Dispose();
        Action again = () => queue.Dispose();
        again.Should().NotThrow();
    }

    [Fact]
    public void Constructor_validates_arguments()
    {
        ((Action)(() => { _ = new EcpCommandQueue(null!); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpCommandQueue("d", capacity: 0); })).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Null_command_throws()
    {
        using var queue = new EcpCommandQueue("dsp-1");
        queue.StartAccepting();
        ((Action)(() => queue.TryEnqueue(null!))).Should().Throw<ArgumentNullException>();
    }
}
