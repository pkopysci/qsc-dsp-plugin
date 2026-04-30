// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.JsonRpc;
using QscDspDevices.TestSupport.Logging;
using Xunit;

namespace QscDspDevices.UnitTests.Protocol;

/// <summary>
/// Unit tests for <see cref="CommandQueue"/>. Verifies the README's
/// FIFO + drop-on-disconnect + refuse-while-disconnected invariants.
/// </summary>
public sealed class CommandQueueTests
{
    [Fact]
    public void TryEnqueue_when_not_accepting_returns_false_and_logs_error()
    {
        using var sink = new TestLoggerSink();
        using var sut = new CommandQueue("dsp-1");

        bool result = sut.TryEnqueue(MakeRequest(1));

        result.Should().BeFalse();
        sut.IsAccepting.Should().BeFalse();
        sink.ContainsErrorMatching("Command attempted while disconnected").Should().BeTrue();
    }

    [Fact]
    public void TryEnqueue_after_StartAccepting_returns_true()
    {
        using var sut = new CommandQueue("dsp-1");
        sut.StartAccepting();

        bool result = sut.TryEnqueue(MakeRequest(1));

        result.Should().BeTrue();
        sut.IsAccepting.Should().BeTrue();
    }

    [Fact]
    public void TryEnqueue_after_Drain_returns_false_again()
    {
        using var sut = new CommandQueue("dsp-1");
        sut.StartAccepting();
        sut.TryEnqueue(MakeRequest(1)).Should().BeTrue();

        sut.Drain();

        sut.TryEnqueue(MakeRequest(2)).Should().BeFalse();
        sut.IsAccepting.Should().BeFalse();
    }

    [Fact]
    public void Drain_discards_pending_entries()
    {
        using var sut = new CommandQueue("dsp-1");
        sut.StartAccepting();
        for (int i = 1; i <= 5; i++)
        {
            sut.TryEnqueue(MakeRequest(i)).Should().BeTrue();
        }

        sut.Drain();

        sut.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public async Task Sequential_enqueue_dequeue_preserves_FIFO_order()
    {
        using var sut = new CommandQueue("dsp-1");
        sut.StartAccepting();
        for (int i = 1; i <= 100; i++)
        {
            sut.TryEnqueue(MakeRequest(i)).Should().BeTrue();
        }

        long[] dequeued = new long[100];
        for (int i = 0; i < 100; i++)
        {
            JsonRpcRequest item = await sut.DequeueAsync(CancellationToken.None);
            dequeued[i] = item.Id;
        }

        dequeued.Should().Equal(Enumerable.Range(1, 100).Select(i => (long)i));
    }

    [Fact]
    public async Task Saturation_drops_oldest_and_increments_drop_counter()
    {
        using var sink = new TestLoggerSink();
        using var sut = new CommandQueue("dsp-1", capacity: 4);
        sut.StartAccepting();

        for (int i = 1; i <= 4; i++)
        {
            sut.TryEnqueue(MakeRequest(i)).Should().BeTrue();
        }

        // Trigger saturation with id=5; the oldest (id=1) should be dropped.
        sut.TryEnqueue(MakeRequest(5)).Should().BeTrue();

        sut.DroppedTotal.Should().Be(1);
        sink.ContainsWarnMatching("queue saturated").Should().BeTrue();

        JsonRpcRequest first = await sut.DequeueAsync(CancellationToken.None);
        first.Id.Should().Be(2);
    }

    [Fact]
    public void TryEnqueue_with_null_throws_ArgumentNullException()
    {
        using var sut = new CommandQueue("dsp-1");

        Action act = () => sut.TryEnqueue(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_with_null_deviceId_throws()
    {
        Action act = () => _ = new CommandQueue(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_with_zero_capacity_throws()
    {
        Action act = () => _ = new CommandQueue("dsp-1", 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Dispose_is_idempotent()
    {
        using var sut = new CommandQueue("dsp-1");
        sut.StartAccepting();

        sut.Dispose();
        Action secondDispose = () => sut.Dispose();
        secondDispose.Should().NotThrow();

        // Enqueue after dispose returns false.
        sut.TryEnqueue(MakeRequest(99)).Should().BeFalse();
    }

    [Fact]
    public void StartAccepting_after_Dispose_throws_ObjectDisposedException()
    {
        using var sut = new CommandQueue("dsp-1");
        sut.Dispose();

        Action act = () => sut.StartAccepting();
        act.Should().Throw<ObjectDisposedException>();
    }

    private static JsonRpcRequest MakeRequest(int id) => new()
    {
        Id = id,
        Method = $"Test.Method{id}",
    };
}
