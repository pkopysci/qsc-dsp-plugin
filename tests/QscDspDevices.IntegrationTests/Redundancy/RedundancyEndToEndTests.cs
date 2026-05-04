// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.AudioControl;
using QscDspDevices.Connectivity;
using QscDspDevices.Connectivity.Redundancy;
using QscDspDevices.LogicTriggers;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.TestSupport.Fakes;
using QscDspDevices.TestSupport.Transport;
using Xunit;

namespace QscDspDevices.IntegrationTests.Redundancy;

/// <summary>
/// End-to-end M6 redundancy test: stand up two FakeQrcServer instances
/// (acting as primary and backup Cores), connect both via the redundant
/// pair, drive a State change on the primary's connection, and verify
/// a subsequent write lands on the backup's wire.
/// </summary>
/// <remarks>
/// State changes are driven directly through each manager's dispatcher
/// (synthetic EngineStatus frame) rather than waiting for FakeQrcServer's
/// auto-push on accept; the auto-push raced the test event-handler under
/// cold-start threadpool starvation (~3 in 10 runs).
/// </remarks>
public sealed class RedundancyEndToEndTests
{
    [Fact]
    public async Task Failover_routes_subsequent_Control_Set_to_the_backup_wire()
    {
        using var env = new RedundancyEnv();

        env.Pair.Connect();

        // Wait for both wires to actually accept TCP. Without this, a
        // TryEnqueue could race the connect and TryEnqueue's "non-
        // accepting" guard would refuse the write.
        await WaitForAsync(
            () => env.Pair.Primary.State == ConnectionState.Connected
                  && env.Pair.Backup.State == ConnectionState.Connected,
            TimeSpan.FromSeconds(15));

        // Drive primary Active synchronously; this avoids the cold-
        // start race that previously made the test flaky waiting for
        // FakeQrcServer's auto-push.
        env.PrimaryDispatcher.Dispatch(
            """{"jsonrpc":"2.0","method":"EngineStatus","params":{"State":"Active"}}""");
        env.Pair.PrimaryDeviceActive.Should().BeTrue();

        env.RoutingQueue.TryEnqueue(new global::QscDspDevices.Protocol.JsonRpc.JsonRpcRequest
        {
            Id = 100,
            Method = "Control.Set",
            Params = new { Name = "test.tag", Value = 1 },
        }).Should().BeTrue();

        await WaitForAsync(
            () => Saw(env.PrimaryServer, "Control.Set", 100),
            TimeSpan.FromSeconds(15));
        Saw(env.BackupServer, "Control.Set", 100).Should().BeFalse(
            "the write should land on the primary's wire, not the backup's");

        // Drive primary into Standby + backup into Active. Both are
        // synchronous through the in-memory dispatcher so there is no
        // wait-for-event race.
        env.BackupDispatcher.Dispatch(
            """{"jsonrpc":"2.0","method":"EngineStatus","params":{"State":"Active"}}""");
        env.PrimaryDispatcher.Dispatch(
            """{"jsonrpc":"2.0","method":"EngineStatus","params":{"State":"Standby"}}""");
        env.Pair.BackupDeviceActive.Should().BeTrue();

        env.RoutingQueue.TryEnqueue(new global::QscDspDevices.Protocol.JsonRpc.JsonRpcRequest
        {
            Id = 200,
            Method = "Control.Set",
            Params = new { Name = "test.tag", Value = 2 },
        }).Should().BeTrue();

        await WaitForAsync(
            () => Saw(env.BackupServer, "Control.Set", 200),
            TimeSpan.FromSeconds(15));
    }

    private static bool Saw(FakeQrcServer server, string method, long id)
    {
        foreach (ReceivedFrame frame in server.GetReceivedFrames())
        {
            if (string.Equals(frame.Method, method, StringComparison.Ordinal) && frame.Id == id)
            {
                return true;
            }
        }

        return false;
    }

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Condition was not met within {timeout.TotalSeconds:0.##}s.");
            }

            await Task.Delay(20);
        }
    }

    /// <summary>
    /// Composes the full M6 stack with two FakeQrcServer instances.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Each ConnectionManager is captured by the RedundantConnectionPair, which disposes them in Pair.Dispose. The Pair is in turn disposed by RedundancyEnv.Dispose.")]
    private sealed class RedundancyEnv : IDisposable
    {
        private readonly IDisposable[] _disposables;

        public RedundancyEnv()
        {
            const string deviceId = "dsp-1";
            PrimaryServer = new FakeQrcServer();
            BackupServer = new FakeQrcServer();
            var primaryTransport = new RawTcpTransport("127.0.0.1", PrimaryServer.Port);
            var backupTransport = new RawTcpTransport("127.0.0.1", BackupServer.Port);
            PrimaryQueue = new CommandQueue(deviceId);
            BackupQueue = new CommandQueue(deviceId);
            PrimaryQueue.StartAccepting();
            BackupQueue.StartAccepting();
            PrimaryDispatcher = new JsonRpcDispatcher(deviceId);
            BackupDispatcher = new JsonRpcDispatcher(deviceId);

            var ids = new IdGenerator();
            var primaryGroup = new ChangeGroupManager(deviceId, ids);
            var backupGroup = new ChangeGroupManager(deviceId, ids);
            RoutingQueue = new RoutingCommandQueue(deviceId);

            var channels = new AudioChannelRegistry(deviceId);
            var zones = new AudioZoneRegistry(deviceId);
            var triggers = new LogicTriggerRegistry(deviceId);
            var routing = new AudioRoutingService(deviceId, channels, RoutingQueue, ids);
            var zone = new AudioZoneEnableService(deviceId, zones, RoutingQueue, ids);
            var trigger = new LogicTriggerService(deviceId, triggers, RoutingQueue, ids);
            var audio = new AudioControlService(deviceId, channels, new LevelScaler(deviceId), RoutingQueue, ids);
            var fanout = new AudioControlServiceFanout(channels, zones, triggers, routing, zone, trigger, audio);

            var primaryManager = new ConnectionManager(
                deviceId, primaryTransport, new ReconnectStrategy(new SystemClock()), PrimaryQueue, PrimaryDispatcher);
            var backupManager = new ConnectionManager(
                deviceId, backupTransport, new ReconnectStrategy(new SystemClock()), BackupQueue, BackupDispatcher);

            Pair = new RedundantConnectionPair(
                deviceId,
                primaryManager,
                PrimaryQueue,
                primaryGroup,
                backupManager,
                BackupQueue,
                backupGroup,
                RoutingQueue,
                fanout,
                SwitchbackPolicy.Default);

            _disposables = new IDisposable[]
            {
                Pair,
                PrimaryQueue,
                BackupQueue,
                RoutingQueue,
                primaryTransport,
                backupTransport,
                PrimaryServer,
                BackupServer,
            };
        }

        public FakeQrcServer PrimaryServer
        {
            get;
        }

        public FakeQrcServer BackupServer
        {
            get;
        }

        public CommandQueue PrimaryQueue
        {
            get;
        }

        public CommandQueue BackupQueue
        {
            get;
        }

        public JsonRpcDispatcher PrimaryDispatcher
        {
            get;
        }

        public JsonRpcDispatcher BackupDispatcher
        {
            get;
        }

        public RoutingCommandQueue RoutingQueue
        {
            get;
        }

        public RedundantConnectionPair Pair
        {
            get;
        }

        public void Dispose()
        {
            foreach (IDisposable d in _disposables)
            {
                try
                {
                    d.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // Already torn down.
                }
            }
        }
    }
}
