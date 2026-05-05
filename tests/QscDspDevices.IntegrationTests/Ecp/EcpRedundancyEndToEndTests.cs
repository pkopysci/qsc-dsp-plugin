// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.Connectivity;
using QscDspDevices.Connectivity.Ecp;
using QscDspDevices.Connectivity.Redundancy;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.Protocol.Ecp;
using QscDspDevices.TestSupport.Fakes;
using QscDspDevices.TestSupport.Transport;
using Xunit;

namespace QscDspDevices.IntegrationTests.Ecp;

/// <summary>
/// End-to-end M-ECP-part-3 redundancy test: stand up two
/// <see cref="FakeEcpServer"/> instances, connect both via the new
/// <see cref="EcpRedundantConnectionPair"/>, drive
/// <see cref="FakeEcpServer.SetActive(bool)"/> on each side, and
/// verify routing flips to the side reporting <c>IS_ACTIVE=1</c>.
/// </summary>
public sealed class EcpRedundancyEndToEndTests
{
    [Fact]
    public async Task Pair_connect_routes_to_primary_when_only_primary_is_Active()
    {
        using var primaryServer = new FakeEcpServer();
        using var backupServer = new FakeEcpServer();
        primaryServer.SetActive(true);
        backupServer.SetActive(false);

        using var env = new RedundancyEnv(primaryServer, backupServer);
        env.Pair.Connect();

        await WaitForAsync(() => env.Pair.PrimaryDeviceActive, TimeSpan.FromSeconds(15));
        env.Pair.PrimaryDeviceActive.Should().BeTrue();
        env.Pair.BackupDeviceActive.Should().BeFalse();
    }

    [Fact]
    public async Task Failover_when_primary_flips_to_Standby_routes_to_backup()
    {
        using var primaryServer = new FakeEcpServer();
        using var backupServer = new FakeEcpServer();
        primaryServer.SetActive(true);
        backupServer.SetActive(false);

        using var env = new RedundancyEnv(primaryServer, backupServer);
        env.Pair.Connect();

        await WaitForAsync(() => env.Pair.PrimaryDeviceActive, TimeSpan.FromSeconds(15));

        // Flip the wires: primary goes Standby, backup goes Active.
        primaryServer.SetActive(false);
        backupServer.SetActive(true);

        await WaitForAsync(() => env.Pair.BackupDeviceActive, TimeSpan.FromSeconds(15));
        env.Pair.PrimaryDeviceActive.Should().BeFalse();
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

            await Task.Delay(50);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "RedundancyEnv.Dispose tears down each component in order; the local variables transferred to fields in the constructor are owned by the env.")]
    private sealed class RedundancyEnv : IDisposable
    {
        private readonly RawTcpTransport _primaryTransport;
        private readonly RawTcpTransport _backupTransport;

        public RedundancyEnv(FakeEcpServer primaryServer, FakeEcpServer backupServer)
        {
            const string deviceId = "dsp-1";
            var primaryTransport = new RawTcpTransport("127.0.0.1", primaryServer.Port);
            var backupTransport = new RawTcpTransport("127.0.0.1", backupServer.Port);
            PrimaryQueue = new EcpCommandQueue(deviceId);
            BackupQueue = new EcpCommandQueue(deviceId);
            PrimaryDispatcher = new EcpDispatcher(deviceId);
            BackupDispatcher = new EcpDispatcher(deviceId);
            RoutingQueue = new EcpRoutingCommandQueue(deviceId);

            var primary = new EcpConnectionManager(
                deviceId,
                primaryTransport,
                new ReconnectStrategy(new SystemClock()),
                PrimaryQueue,
                PrimaryDispatcher,
                credentialsSource: () => null);
            var backup = new EcpConnectionManager(
                deviceId,
                backupTransport,
                new ReconnectStrategy(new SystemClock()),
                BackupQueue,
                BackupDispatcher,
                credentialsSource: () => null);

            Pair = new EcpRedundantConnectionPair(deviceId, primary, backup, RoutingQueue, SwitchbackPolicy.Default);
            _primaryTransport = primaryTransport;
            _backupTransport = backupTransport;
        }

        public EcpCommandQueue PrimaryQueue
        {
            get;
        }

        public EcpCommandQueue BackupQueue
        {
            get;
        }

        public EcpDispatcher PrimaryDispatcher
        {
            get;
        }

        public EcpDispatcher BackupDispatcher
        {
            get;
        }

        public EcpRoutingCommandQueue RoutingQueue
        {
            get;
        }

        public EcpRedundantConnectionPair Pair
        {
            get;
        }

        public void Dispose()
        {
            try
            {
                Pair.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already torn down.
            }

            PrimaryQueue.Dispose();
            BackupQueue.Dispose();
            RoutingQueue.Dispose();
            _primaryTransport.Dispose();
            _backupTransport.Dispose();
        }
    }
}
