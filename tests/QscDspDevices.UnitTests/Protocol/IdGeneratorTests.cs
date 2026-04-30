// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.Protocol;
using Xunit;

namespace QscDspDevices.UnitTests.Protocol;

/// <summary>
/// Unit tests for <see cref="IdGenerator"/>.
/// </summary>
public sealed class IdGeneratorTests
{
    [Fact]
    public void First_call_returns_one()
    {
        var sut = new IdGenerator();
        sut.Next().Should().Be(1L);
    }

    [Fact]
    public void Subsequent_calls_increase_strictly()
    {
        var sut = new IdGenerator();
        long[] ids = Enumerable.Range(0, 10).Select(_ => sut.Next()).ToArray();
        ids.Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Concurrent_callers_observe_no_duplicate_ids()
    {
        var sut = new IdGenerator();
        const int threads = 8;
        const int callsPerThread = 1000;

        var bag = new System.Collections.Concurrent.ConcurrentBag<long>();
        Task[] tasks = Enumerable.Range(0, threads).Select(_ =>
            Task.Run(() =>
            {
                for (int i = 0; i < callsPerThread; i++)
                {
                    bag.Add(sut.Next());
                }
            })).ToArray();
        await Task.WhenAll(tasks);

        bag.Should().HaveCount(threads * callsPerThread);
        bag.Distinct().Count().Should().Be(threads * callsPerThread);
    }
}
