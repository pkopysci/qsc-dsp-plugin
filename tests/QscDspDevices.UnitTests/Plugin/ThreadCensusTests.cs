// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.Plugin.Threading;
using Xunit;

namespace QscDspDevices.UnitTests.Plugin;

/// <summary>
/// Unit tests for <see cref="ThreadCensus"/>. The token-based API
/// (Register returns a disposable handle) replaces the earlier thread-id-
/// keyed design, which was unsound across `await` boundaries; these tests
/// pin the new contract.
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
    public void Register_returns_a_handle_whose_Dispose_removes_the_entry()
    {
        var sut = new ThreadCensus("dsp-1");

        ThreadCensusRegistration reg = sut.Register("send");
        sut.AliveCount.Should().Be(1);
        sut.Snapshot().Should().Contain("send");
        reg.IsBudgetBreach.Should().BeFalse();

        reg.Dispose();
        sut.AliveCount.Should().Be(0);
    }

    [Fact]
    public void Disposing_a_registration_is_idempotent()
    {
        var sut = new ThreadCensus("dsp-1");
        ThreadCensusRegistration reg = sut.Register("send");

        reg.Dispose();
        Action secondDispose = () => reg.Dispose();
        secondDispose.Should().NotThrow();
        sut.AliveCount.Should().Be(0);
    }

    [Fact]
    public void Register_with_null_role_throws()
    {
        var sut = new ThreadCensus("dsp-1");
        Action act = () => sut.Register(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Three_concurrent_registrations_all_succeed_and_release_correctly()
    {
        var sut = new ThreadCensus("dsp-1");
        using var entered = new SemaphoreSlim(0, 3);
        using var release = new SemaphoreSlim(0, 3);

        Task[] tasks = Enumerable.Range(0, 3).Select(i => Task.Run(async () =>
        {
            // Acquire the registration on this task, then await across a
            // potential thread switch before disposing it. The token-based
            // API guarantees correct release regardless of which threadpool
            // worker resumes.
            ThreadCensusRegistration reg = sut.Register($"role-{i}");
            entered.Release();
            await release.WaitAsync(TimeSpan.FromSeconds(5));
            reg.Dispose();
        })).ToArray();

        for (int i = 0; i < 3; i++)
        {
            await entered.WaitAsync(TimeSpan.FromSeconds(5));
        }

        sut.AliveCount.Should().Be(3);

        release.Release(3);
        await Task.WhenAll(tasks);

        sut.AliveCount.Should().Be(0);
    }

    [Fact]
    public async Task Async_registration_survives_await_boundaries()
    {
        // Pins the regression that motivated the API change: an async
        // method that registers, awaits across a threadpool boundary,
        // then disposes. The token-based API correctly releases even
        // when Dispose runs on a different thread than Register.
        var sut = new ThreadCensus("dsp-1");

        await Task.Run(async () =>
        {
            ThreadCensusRegistration reg = sut.Register("async-work");
            sut.AliveCount.Should().Be(1);

            // Force a thread switch via a real (tiny) delay.
            await Task.Yield();
            await Task.Delay(10);
            await Task.Yield();

            reg.Dispose();
        });

        sut.AliveCount.Should().Be(0);
    }

    [Fact]
    public void Breach_sentinel_isBudgetBreach_is_true_and_dispose_is_a_no_op()
    {
        ThreadCensusRegistration breach = ThreadCensusRegistration.Breach;

        breach.IsBudgetBreach.Should().BeTrue();
        Action act = () => breach.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Two_registrations_from_the_same_census_are_inequal()
    {
        var sut = new ThreadCensus("dsp-1");
        ThreadCensusRegistration a = sut.Register("a");
        ThreadCensusRegistration b = sut.Register("b");

        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();
        a.Equals(b).Should().BeFalse();
        a.Equals((object)b).Should().BeFalse();
        a.GetHashCode().Should().NotBe(b.GetHashCode());

        a.Dispose();
        b.Dispose();
    }

    [Fact]
    public void A_registration_equals_itself()
    {
        var sut = new ThreadCensus("dsp-1");
        ThreadCensusRegistration a = sut.Register("a");
        ThreadCensusRegistration copy = a;

        (a == copy).Should().BeTrue();
        (a != copy).Should().BeFalse();
        a.Equals((object)copy).Should().BeTrue();
        a.Equals("not a registration").Should().BeFalse();

        a.Dispose();
    }
}
