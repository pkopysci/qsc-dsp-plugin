// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.Connectivity;
using QscDspDevices.Connectivity.Ecp;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.Protocol.Ecp;
using QscDspDevices.TestSupport.Fakes;
using QscDspDevices.TestSupport.Transport;
using Xunit;

namespace QscDspDevices.IntegrationTests.Ecp;

/// <summary>
/// End-to-end tests for the ECP backend over a real TCP transport
/// driven by <see cref="FakeEcpServer"/>.
/// </summary>
public sealed class EcpFakeServerEndToEndTests
{
    [Fact]
    public async Task Connect_then_sg_round_trips_through_dispatcher()
    {
        using var server = new FakeEcpServer();
        using var transport = new RawTcpTransport("127.0.0.1", server.Port);
        using var queue = new EcpCommandQueue("dsp-1");
        var dispatcher = new EcpDispatcher("dsp-1");
        using var manager = new EcpConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(new SystemClock()),
            queue,
            dispatcher,
            credentialsSource: () => null);

        var status = new TaskCompletionSource<EcpResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.ResponseReceived += (_, args) =>
        {
            if (args.Arg.Kind == EcpResponseKind.StatusReport)
            {
                status.TrySetResult(args.Arg);
            }
        };

        manager.Connect();
        await WaitForAsync(() => manager.State == ConnectionState.Connected && queue.IsAccepting, TimeSpan.FromSeconds(15));

        queue.TryEnqueue(EcpCommand.StatusGet()).Should().BeTrue();

        Task winner = await Task.WhenAny(status.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        winner.Should().Be(status.Task);

        EcpResponse sr = await status.Task;
        sr.IsActive.Should().Be(1);
        sr.IsPrimary.Should().Be(1);
        sr.DesignName.Should().Be("TestDesign");
    }

    [Fact]
    public async Task Set_then_Get_round_trips_through_csv_echo()
    {
        using var server = new FakeEcpServer();
        using var transport = new RawTcpTransport("127.0.0.1", server.Port);
        using var queue = new EcpCommandQueue("dsp-1");
        var dispatcher = new EcpDispatcher("dsp-1");
        using var manager = new EcpConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(new SystemClock()),
            queue,
            dispatcher,
            credentialsSource: () => null);

        var cv = new TaskCompletionSource<EcpResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.ResponseReceived += (_, args) =>
        {
            if (args.Arg.Kind == EcpResponseKind.ControlValue && args.Arg.ControlId == "Output.gain")
            {
                cv.TrySetResult(args.Arg);
            }
        };

        manager.Connect();
        await WaitForAsync(() => manager.State == ConnectionState.Connected && queue.IsAccepting, TimeSpan.FromSeconds(15));

        queue.TryEnqueue(EcpCommand.ControlSetValue("Output.gain", -10.5)).Should().BeTrue();

        Task winner = await Task.WhenAny(cv.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        winner.Should().Be(cv.Task);

        EcpResponse echo = await cv.Task;
        echo.Value.Should().Be(-10.5);
    }

    [Fact]
    public async Task Login_required_handshake_succeeds_with_correct_credentials()
    {
        using var server = new FakeEcpServer();
        server.RequireLogin("alice", "1234");

        using var transport = new RawTcpTransport("127.0.0.1", server.Port);
        using var queue = new EcpCommandQueue("dsp-1");
        var dispatcher = new EcpDispatcher("dsp-1");
        using var manager = new EcpConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(new SystemClock()),
            queue,
            dispatcher,
            credentialsSource: () => new EcpCredentials("alice", "1234"));

        // The manager itself owns the handshake — the test does NOT
        // subscribe to login_required. Reaching Connected proves the
        // manager sent the login command, the Core replied with
        // login_success, and the post-auth StartAccepting ran.
        manager.Connect();
        await WaitForAsync(() => manager.State == ConnectionState.Connected && queue.IsAccepting, TimeSpan.FromSeconds(15));

        // Verify we actually authed (rather than slipping past on the
        // anonymous-mode 500ms timeout): the server must have observed
        // a login command in its received-command log.
        IReadOnlyList<string> received = server.GetReceivedCommands();
        received.Should().Contain(c => c.StartsWith("login ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Login_failed_never_reaches_Connected()
    {
        using var server = new FakeEcpServer();
        server.RequireLogin("alice", "1234");

        using var transport = new RawTcpTransport("127.0.0.1", server.Port);
        using var queue = new EcpCommandQueue("dsp-1");
        var dispatcher = new EcpDispatcher("dsp-1");
        using var manager = new EcpConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(new SystemClock()),
            queue,
            dispatcher,
            credentialsSource: () => new EcpCredentials("alice", "wrong"));

        var loginFailed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.ResponseReceived += (_, args) =>
        {
            if (args.Arg.Kind == EcpResponseKind.LoginFailed)
            {
                loginFailed.TrySetResult(true);
            }
        };

        manager.Connect();

        // The manager must observe login_failed and never transition
        // to Connected. Asserting "did not reach Connected within 5s"
        // is racy across reconnect cycles, so instead pin the
        // affirmative observation: login_failed was received from the
        // Core, AND the queue stays non-accepting at that point.
        Task winner = await Task.WhenAny(loginFailed.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        winner.Should().Be(loginFailed.Task);
        queue.IsAccepting.Should().BeFalse();
    }

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Condition not met within {timeout.TotalSeconds:0.##}s.");
            }

            await Task.Delay(20);
        }
    }
}
