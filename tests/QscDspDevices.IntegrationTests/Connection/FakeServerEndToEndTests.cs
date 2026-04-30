// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.Connectivity;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.Protocol;
using QscDspDevices.TestSupport.Fakes;
using QscDspDevices.TestSupport.Logging;
using QscDspDevices.TestSupport.Transport;
using Xunit;

namespace QscDspDevices.IntegrationTests.Connection;

/// <summary>
/// End-to-end integration tests that wire the production
/// <see cref="ConnectionManager"/> + <see cref="JsonRpcDispatcher"/> +
/// <see cref="CommandQueue"/> against the in-process
/// <see cref="FakeQrcServer"/> via <c>RawTcpTransport</c>.
/// </summary>
/// <remarks>
/// These tests use the real <see cref="SystemClock"/> rather than the
/// deterministic clock so the network stack actually runs end-to-end.
/// They are kept fast by avoiding the 15-second reconnect path here —
/// the deterministic-clock tests in <c>QscDspDevices.UnitTests</c>
/// cover that timing.
/// </remarks>
public sealed class FakeServerEndToEndTests
{
    [Fact]
    public async Task Connecting_to_a_FakeQrcServer_drives_state_into_Connected()
    {
        using var server = new FakeQrcServer();
        using var transport = new RawTcpTransport("127.0.0.1", server.Port);
        using var queue = new CommandQueue("dsp-1");
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var clock = new SystemClock();
        using var manager = new ConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(clock),
            queue,
            dispatcher);

        manager.Connect();

        await WaitForStateAsync(manager, ConnectionState.Connected, TimeSpan.FromSeconds(5));
        queue.IsAccepting.Should().BeTrue();
        server.ConnectedClientCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Server_drop_triggers_state_back_into_Connecting_on_reconnect()
    {
        using var server = new FakeQrcServer();
        using var transport = new RawTcpTransport("127.0.0.1", server.Port);
        using var queue = new CommandQueue("dsp-1");
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var clock = new SystemClock();
        using var manager = new ConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(clock),
            queue,
            dispatcher);

        manager.Connect();
        await WaitForStateAsync(manager, ConnectionState.Connected, TimeSpan.FromSeconds(5));

        server.DropConnection();

        // Per the README the manager schedules a reconnect 15s out. We
        // do not wait the full 15s in this integration test (the timing
        // is exercised by ConnectionManagerTests.Failed_first_attempt_*
        // with the deterministic clock); we simply verify the state
        // moved out of Connected.
        await WaitForStateChangeAwayFromAsync(manager, ConnectionState.Connected, TimeSpan.FromSeconds(5));

        // The queue must have refused after the disconnect was observed.
        queue.IsAccepting.Should().BeFalse();
    }

    [Fact]
    public async Task Disconnect_drains_queue_and_returns_to_Disconnected()
    {
        using var server = new FakeQrcServer();
        using var transport = new RawTcpTransport("127.0.0.1", server.Port);
        using var queue = new CommandQueue("dsp-1");
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        using var manager = new ConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(new SystemClock()),
            queue,
            dispatcher);

        manager.Connect();
        await WaitForStateAsync(manager, ConnectionState.Connected, TimeSpan.FromSeconds(5));

        // Enqueue a few requests to ensure they are drained on disconnect.
        for (int i = 1; i <= 3; i++)
        {
            queue.TryEnqueue(new Protocol.JsonRpc.JsonRpcRequest { Id = i, Method = "Test" }).Should().BeTrue();
        }

        manager.Disconnect();
        await manager.WaitForDisconnectedAsync(TimeSpan.FromSeconds(5));

        manager.State.Should().Be(ConnectionState.Disconnected);
        queue.IsAccepting.Should().BeFalse();
        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public async Task Refusing_send_while_disconnected_logs_an_error()
    {
        using var sink = new TestLoggerSink();
        using var server = new FakeQrcServer();
        using var transport = new RawTcpTransport("127.0.0.1", server.Port);
        using var queue = new CommandQueue("dsp-1");
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        using var manager = new ConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(new SystemClock()),
            queue,
            dispatcher);

        // Without ever calling Connect, the queue must refuse + log.
        bool ok = queue.TryEnqueue(new Protocol.JsonRpc.JsonRpcRequest { Id = 1, Method = "Component.Set" });

        ok.Should().BeFalse();
        sink.ContainsErrorMatching("Command attempted while disconnected").Should().BeTrue();
        await Task.CompletedTask;
    }

    private static async Task WaitForStateAsync(ConnectionManager manager, ConnectionState desired, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (manager.State != desired)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Manager did not reach {desired} within {timeout.TotalSeconds:0.##}s; current state is {manager.State}.");
            }

            await Task.Delay(20);
        }
    }

    private static async Task WaitForStateChangeAwayFromAsync(ConnectionManager manager, ConnectionState fromState, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (manager.State == fromState)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Manager did not move out of {fromState} within {timeout.TotalSeconds:0.##}s.");
            }

            await Task.Delay(20);
        }
    }
}
