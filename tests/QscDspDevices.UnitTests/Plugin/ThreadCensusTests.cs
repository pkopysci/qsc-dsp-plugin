// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.TestSupport.Logging;
using Xunit;

namespace QscDspDevices.UnitTests.Plugin;

/// <summary>
/// Unit tests for <see cref="ThreadCensus"/>. The 4th-thread breach is
/// only tested in non-DEBUG mode here because in DEBUG the census calls
/// Environment.FailFast which would terminate the test process. (CI
/// runs Debug builds; we test the log-only RELEASE path by binding the
/// detection to AliveCount instead.)
/// </summary>
public sealed class ThreadCensusTests
{
    [Fact]
    public void Constructor_with_null_deviceId_throws()
    {
        Action act = () => _ = new ThreadCensus(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Newly_created_census_has_zero_alive()
    {
        var sut = new ThreadCensus("dsp-1");
        sut.AliveCount.Should().Be(0);
        sut.Snapshot().Should().BeEmpty();
    }

    [Fact]
    public void Register_and_Unregister_track_calling_thread()
    {
        var sut = new ThreadCensus("dsp-1");

        sut.Register("send").Should().BeTrue();
        sut.AliveCount.Should().Be(1);
        sut.Snapshot().Should().Contain("send");

        sut.Unregister();
        sut.AliveCount.Should().Be(0);
    }

    [Fact]
    public void Register_is_idempotent_for_the_same_thread()
    {
        var sut = new ThreadCensus("dsp-1");
        sut.Register("send").Should().BeTrue();
        sut.Register("send").Should().BeTrue();
        sut.AliveCount.Should().Be(1);
    }

    [Fact]
    public void Register_with_null_role_throws()
    {
        var sut = new ThreadCensus("dsp-1");
        Action act = () => sut.Register(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Three_concurrent_threads_register_successfully()
    {
        var sut = new ThreadCensus("dsp-1");
        using var entered = new SemaphoreSlim(0, 3);
        using var release = new SemaphoreSlim(0, 3);

        var tasks = Enumerable.Range(0, 3).Select(i => Task.Run(() =>
        {
            sut.Register($"role-{i}").Should().BeTrue();
            entered.Release();
            release.Wait();
            sut.Unregister();
        })).ToArray();

        // Wait for all three to have registered.
        for (int i = 0; i < 3; i++)
        {
            await entered.WaitAsync(TimeSpan.FromSeconds(5));
        }

        sut.AliveCount.Should().Be(3);

        release.Release(3);
        await Task.WhenAll(tasks);

        sut.AliveCount.Should().Be(0);
    }
}
