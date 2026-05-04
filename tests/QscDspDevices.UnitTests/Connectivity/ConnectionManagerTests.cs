// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.Connectivity;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.JsonRpc;
using QscDspDevices.TestSupport.Logging;
using QscDspDevices.TestSupport.Time;
using QscDspDevices.TestSupport.Transport;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity;

/// <summary>
/// Unit tests for <see cref="ConnectionManager"/>. Validates the README's
/// Disconnected -> Connecting -> Connected -> Disconnecting -> Disconnected
/// state machine, the 15s reconnect loop, queue draining on disconnect,
/// and the IsOnline-before-NotifyOnlineStatus ordering invariant
/// (here exposed as the StateChanged event firing AFTER queue.IsAccepting
/// flips, which the QscDspTcp wrapper translates to BaseDevice).
/// </summary>
public sealed class ConnectionManagerTests
{
    private static readonly string[] ExpectedRoles = { "session", "send", "keepalive" };

    [Fact]
    public async Task Connect_drives_state_through_Connecting_to_Connected()
    {
        using var transport = new StubTransport();
        var clock = new DeterministicClock();
        using var queue = new CommandQueue("dsp-1");
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        using var sut = new ConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(clock),
            queue,
            dispatcher);

        var observed = new List<ConnectionState>();
        sut.StateChanged += (_, args) => observed.Add(args.Arg);

        sut.Connect();

        // Wait for Connecting transition.
        await WaitForStateAsync(sut, ConnectionState.Connecting);
        transport.SimulateConnectSuccess();

        await WaitForStateAsync(sut, ConnectionState.Connected);

        observed.Should().ContainInOrder(ConnectionState.Connecting, ConnectionState.Connected);
        queue.IsAccepting.Should().BeTrue();
    }

    [Fact]
    public async Task Connected_steady_state_registers_three_threadcensus_roles()
    {
        // Pins README §4 + spec threading-budget: ≤ 3 concurrent
        // plugin-owned tasks. Roles are session, send, keepalive.
        // Keepalive only starts when both clock + ids are supplied,
        // so we pass them here.
        using var transport = new StubTransport();
        var clock = new DeterministicClock();
        var ids = new IdGenerator();
        using var queue = new CommandQueue("dsp-1");
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        using var sut = new ConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(clock),
            queue,
            dispatcher,
            clock: clock,
            ids: ids);

        sut.Connect();
        await WaitForStateAsync(sut, ConnectionState.Connecting);
        transport.SimulateConnectSuccess();
        await WaitForStateAsync(sut, ConnectionState.Connected);

        // The send + keepalive loops register lazily on first iteration.
        // Wait until the census reports the steady-state count.
        await WaitForAsync(
            () => sut.ThreadCensus.AliveCount == 3,
            TimeSpan.FromSeconds(15));

        IReadOnlyList<string> roles = sut.ThreadCensus.Snapshot();
        roles.Should().BeEquivalentTo(ExpectedRoles);
    }

    [Fact]
    public async Task Disconnect_drains_queue_and_reaches_Disconnected()
    {
        using var transport = new StubTransport();
        var clock = new DeterministicClock();
        using var queue = new CommandQueue("dsp-1");
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        using var sut = new ConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(clock),
            queue,
            dispatcher);

        sut.Connect();
        await WaitForStateAsync(sut, ConnectionState.Connecting);
        transport.SimulateConnectSuccess();
        await WaitForStateAsync(sut, ConnectionState.Connected);

        // Enqueue some pending requests to confirm the drain works.
        queue.TryEnqueue(new JsonRpcRequest { Id = 1, Method = "Test1" }).Should().BeTrue();
        queue.TryEnqueue(new JsonRpcRequest { Id = 2, Method = "Test2" }).Should().BeTrue();

        sut.Disconnect();
        await sut.WaitForDisconnectedAsync(TimeSpan.FromSeconds(5));

        sut.State.Should().Be(ConnectionState.Disconnected);
        queue.IsAccepting.Should().BeFalse();
        queue.SnapshotPending().Should().BeEmpty();
        transport.DisconnectCallCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Failed_first_attempt_triggers_reconnect_after_exactly_fifteen_seconds()
    {
        using var transport = new StubTransport();
        var clock = new DeterministicClock();
        using var queue = new CommandQueue("dsp-1");
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        using var sink = new TestLoggerSink();
        using var sut = new ConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(clock),
            queue,
            dispatcher);

        // Subscribe BEFORE the failure so we don't miss the waiter-added
        // signal. The session task registers a clock waiter inside
        // ReconnectStrategy.WaitForNextAttemptAsync after a failed attempt;
        // gating Advance() on that registration avoids advancing time
        // before the production code is ready to wait for it.
        Task waiterAdded = clock.WhenNextWaiterAddedAsync();

        sut.Connect();

        await WaitForStateAsync(sut, ConnectionState.Connecting);
        transport.SimulateConnectFailure("first attempt fails");

        // After failure, manager should schedule a reconnect 15s out. We
        // verify by counting Connect calls before and after the advance.
        await WaitForConnectCountAsync(transport, 1);

        await waiterAdded.WaitAsync(TimeSpan.FromSeconds(15));

        // Advance only 14s — no second Connect yet.
        clock.Advance(TimeSpan.FromSeconds(14));
        await Task.Delay(50);
        transport.ConnectCallCount.Should().Be(1);

        // Cross the 15s threshold.
        clock.Advance(TimeSpan.FromSeconds(1));
        await WaitForConnectCountAsync(transport, 2);

        // Now succeed.
        transport.SimulateConnectSuccess();
        await WaitForStateAsync(sut, ConnectionState.Connected);

        sink.Captures.Should().Contain(
            c => c.Message.Contains("Connection attempt failed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Mid_flight_drop_triggers_reconnect_loop()
    {
        using var transport = new StubTransport();
        var clock = new DeterministicClock();
        using var queue = new CommandQueue("dsp-1");
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        using var sut = new ConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(clock),
            queue,
            dispatcher);

        sut.Connect();
        await WaitForStateAsync(sut, ConnectionState.Connecting);
        transport.SimulateConnectSuccess();
        await WaitForStateAsync(sut, ConnectionState.Connected);

        // Subscribe BEFORE the drop so we don't miss the waiter-added
        // signal. After the drop, the session loop reaches
        // ReconnectStrategy.WaitForNextAttemptAsync and registers a
        // 15-second clock waiter; we wait on that registration before
        // advancing virtual time.
        Task waiterAdded = clock.WhenNextWaiterAddedAsync();
        transport.SimulateMidFlightDrop("cable pulled");

        await waiterAdded.WaitAsync(TimeSpan.FromSeconds(15));
        clock.Advance(TimeSpan.FromSeconds(15));
        await WaitForConnectCountAsync(transport, 2);

        transport.SimulateConnectSuccess();
        await WaitForStateAsync(sut, ConnectionState.Connected);
    }

    [Fact]
    public void Connect_when_already_Connected_is_a_no_op()
    {
        using var transport = new StubTransport();
        var clock = new DeterministicClock();
        using var queue = new CommandQueue("dsp-1");
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        using var sut = new ConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(clock),
            queue,
            dispatcher);

        sut.Connect();
        sut.Connect();
        sut.Connect();

        // The transport should see at most one Connect call.
        transport.ConnectCallCount.Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public void Disconnect_before_Connect_is_a_no_op()
    {
        using var transport = new StubTransport();
        var clock = new DeterministicClock();
        using var queue = new CommandQueue("dsp-1");
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        using var sut = new ConnectionManager(
            "dsp-1",
            transport,
            new ReconnectStrategy(clock),
            queue,
            dispatcher);

        sut.Disconnect();
        sut.State.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public void Constructor_rejects_null_args()
    {
        using var transport = new StubTransport();
        var clock = new DeterministicClock();
        using var queue = new CommandQueue("dsp-1");
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var reconnect = new ReconnectStrategy(clock);

        Action a = () => _ = new ConnectionManager(null!, transport, reconnect, queue, dispatcher);
        Action b = () => _ = new ConnectionManager("dsp-1", null!, reconnect, queue, dispatcher);
        Action c = () => _ = new ConnectionManager("dsp-1", transport, null!, queue, dispatcher);
        Action d = () => _ = new ConnectionManager("dsp-1", transport, reconnect, null!, dispatcher);
        Action e = () => _ = new ConnectionManager("dsp-1", transport, reconnect, queue, null!);

        a.Should().Throw<ArgumentNullException>();
        b.Should().Throw<ArgumentNullException>();
        c.Should().Throw<ArgumentNullException>();
        d.Should().Throw<ArgumentNullException>();
        e.Should().Throw<ArgumentNullException>();
    }

    private static async Task WaitForStateAsync(ConnectionManager manager, ConnectionState desired, int timeoutMs = 10000)
    {
        // Event-driven: subscribe to StateChanged, then snapshot current
        // state under the same lock the manager uses (well — actually we
        // just check after subscribing; if we missed the desired state
        // the subscription will catch the next transition; if we already
        // matched, return immediately). This eliminates the cold-pool
        // race where Task.Delay polling lost to a starved threadpool.
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<gcu_common_utils.GenericEventArgs.GenericSingleEventArgs<ConnectionState>> handler = (_, args) =>
        {
            if (args.Arg == desired)
            {
                tcs.TrySetResult(true);
            }
        };
        manager.StateChanged += handler;
        try
        {
            if (manager.State == desired)
            {
                return;
            }

            using var cts = new CancellationTokenSource(timeoutMs);
            using (cts.Token.Register(() => tcs.TrySetException(
                new TimeoutException($"Manager did not reach {desired} within {timeoutMs}ms; current state is {manager.State}."))))
            {
                await tcs.Task;
            }
        }
        finally
        {
            manager.StateChanged -= handler;
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

    private static async Task WaitForConnectCountAsync(StubTransport transport, int desired, int timeoutMs = 10000)
    {
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);
        while (transport.ConnectCallCount < desired)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Transport.ConnectCallCount did not reach {desired} within {timeoutMs}ms; current is {transport.ConnectCallCount}.");
            }

            await Task.Delay(10);
        }
    }
}
