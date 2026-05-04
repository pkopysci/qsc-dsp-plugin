// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.Connectivity;
using QscDspDevices.Connectivity.Ecp;
using QscDspDevices.Protocol.Ecp;
using QscDspDevices.TestSupport.Time;
using QscDspDevices.TestSupport.Transport;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity.Ecp;

public sealed class EcpConnectionManagerTests
{
    private static readonly string[] ExpectedRoles = { "session", "send", "keepalive" };

    [Fact]
    public async Task Connect_drives_state_through_Connecting_to_Connected()
    {
        using var transport = new StubTransport();
        var clock = new DeterministicClock();
        using var queue = new EcpCommandQueue("dsp-1");
        var dispatcher = new EcpDispatcher("dsp-1");
        using var sut = new EcpConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(clock),
            queue,
            dispatcher,
            credentialsSource: () => null);

        sut.Connect();
        await WaitForStateAsync(sut, ConnectionState.Connecting);
        transport.SimulateConnectSuccess();
        await WaitForStateAsync(sut, ConnectionState.Connected);

        queue.IsAccepting.Should().BeTrue();
    }

    [Fact]
    public async Task Disconnect_drains_queue_and_reaches_Disconnected()
    {
        using var transport = new StubTransport();
        var clock = new DeterministicClock();
        using var queue = new EcpCommandQueue("dsp-1");
        var dispatcher = new EcpDispatcher("dsp-1");
        using var sut = new EcpConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(clock),
            queue,
            dispatcher,
            credentialsSource: () => null);

        sut.Connect();
        await WaitForStateAsync(sut, ConnectionState.Connecting);
        transport.SimulateConnectSuccess();
        await WaitForStateAsync(sut, ConnectionState.Connected);

        queue.TryEnqueue("sg").Should().BeTrue();

        sut.Disconnect();
        await sut.WaitForDisconnectedAsync(TimeSpan.FromSeconds(15));

        sut.State.Should().Be(ConnectionState.Disconnected);
        queue.IsAccepting.Should().BeFalse();
    }

    [Fact]
    public async Task Connected_steady_state_registers_three_threadcensus_roles()
    {
        using var transport = new StubTransport();
        var clock = new DeterministicClock();
        using var queue = new EcpCommandQueue("dsp-1");
        var dispatcher = new EcpDispatcher("dsp-1");
        using var sut = new EcpConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(clock),
            queue,
            dispatcher,
            credentialsSource: () => null);

        sut.Connect();
        await WaitForStateAsync(sut, ConnectionState.Connecting);
        transport.SimulateConnectSuccess();
        await WaitForStateAsync(sut, ConnectionState.Connected);

        await WaitForAsync(() => sut.ThreadCensus.AliveCount == 3, TimeSpan.FromSeconds(30));
        sut.ThreadCensus.Snapshot().Should().BeEquivalentTo(ExpectedRoles);
    }

    [Fact]
    public async Task RxReceived_dispatch_routes_through_framer_and_dispatcher()
    {
        using var transport = new StubTransport();
        var clock = new DeterministicClock();
        using var queue = new EcpCommandQueue("dsp-1");
        var dispatcher = new EcpDispatcher("dsp-1");
        using var sut = new EcpConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(clock),
            queue,
            dispatcher,
            credentialsSource: () => null);

        var received = new List<EcpResponse>();
        dispatcher.ResponseReceived += (_, args) => received.Add(args.Arg);

        sut.Connect();
        await WaitForStateAsync(sut, ConnectionState.Connecting);
        transport.SimulateConnectSuccess();
        await WaitForStateAsync(sut, ConnectionState.Connected);

        transport.SimulateReceive(System.Text.Encoding.UTF8.GetBytes("sr \"d\" \"id\" 1 1\r\n"));
        await WaitForAsync(() => received.Count >= 1, TimeSpan.FromSeconds(15));

        received.Should().Contain(r => r.Kind == EcpResponseKind.StatusReport);
    }

    [Fact]
    public async Task Send_drains_queue_to_transport()
    {
        using var transport = new StubTransport();
        var clock = new DeterministicClock();
        using var queue = new EcpCommandQueue("dsp-1");
        var dispatcher = new EcpDispatcher("dsp-1");
        using var sut = new EcpConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(clock),
            queue,
            dispatcher,
            credentialsSource: () => null);

        sut.Connect();
        await WaitForStateAsync(sut, ConnectionState.Connecting);
        transport.SimulateConnectSuccess();
        await WaitForStateAsync(sut, ConnectionState.Connected);

        queue.TryEnqueue("sg").Should().BeTrue();
        await WaitForAsync(() => transport.SentPayloads.Count >= 1, TimeSpan.FromSeconds(15));

        // Wire bytes are "sg\n".
        transport.SentPayloads[0].Should().Equal(0x73, 0x67, 0x0A);
    }

    [Fact]
    public void Constructor_validates_required_arguments()
    {
        using var transport = new StubTransport();
        var clock = new DeterministicClock();
        using var queue = new EcpCommandQueue("dsp-1");
        var dispatcher = new EcpDispatcher("dsp-1");
        var reconnect = new ReconnectStrategy(clock);

        ((Action)(() => { _ = new EcpConnectionManager(null!, transport, reconnect, queue, dispatcher, () => null); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpConnectionManager("d", null!, reconnect, queue, dispatcher, () => null); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpConnectionManager("d", transport, null!, queue, dispatcher, () => null); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpConnectionManager("d", transport, reconnect, null!, dispatcher, () => null); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpConnectionManager("d", transport, reconnect, queue, null!, () => null); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpConnectionManager("d", transport, reconnect, queue, dispatcher, null!); })).Should().Throw<ArgumentNullException>();
    }

    private static async Task WaitForStateAsync(EcpConnectionManager sut, ConnectionState desired, int timeoutMs = 15000)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<gcu_common_utils.GenericEventArgs.GenericSingleEventArgs<ConnectionState>> handler = (_, args) =>
        {
            if (args.Arg == desired)
            {
                tcs.TrySetResult(true);
            }
        };
        sut.StateChanged += handler;
        try
        {
            if (sut.State == desired)
            {
                return;
            }

            using var cts = new CancellationTokenSource(timeoutMs);
            using (cts.Token.Register(() => tcs.TrySetException(
                new TimeoutException($"EcpConnectionManager did not reach {desired} within {timeoutMs}ms; current state is {sut.State}."))))
            {
                await tcs.Task;
            }
        }
        finally
        {
            sut.StateChanged -= handler;
        }
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
