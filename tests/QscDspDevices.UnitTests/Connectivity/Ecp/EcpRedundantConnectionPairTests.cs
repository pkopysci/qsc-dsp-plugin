// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.Connectivity;
using QscDspDevices.Connectivity.Ecp;
using QscDspDevices.Connectivity.Redundancy;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.Ecp;
using QscDspDevices.TestSupport.Time;
using QscDspDevices.TestSupport.Transport;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity.Ecp;

public sealed class EcpRedundantConnectionPairTests
{
    [Fact]
    public void New_pair_with_no_engine_state_pushes_reports_no_active()
    {
        using var env = new PairEnv();
        env.Pair.PrimaryDeviceActive.Should().BeFalse();
        env.Pair.BackupDeviceActive.Should().BeFalse();
        env.Pair.ActiveSlot.Should().BeNull();
    }

    [Fact]
    public void Primary_sr_with_IsActive_1_promotes_primary()
    {
        using var env = new PairEnv();

        env.PrimaryDispatcher.Dispatch("sr \"d\" \"id\" 1 1");
        env.Pair.PrimaryDeviceActive.Should().BeTrue();
    }

    [Fact]
    public void Failover_when_primary_goes_Standby_and_backup_is_Active()
    {
        using var env = new PairEnv();

        env.PrimaryDispatcher.Dispatch("sr \"d\" \"id\" 1 1");
        env.BackupDispatcher.Dispatch("sr \"d\" \"id\" 0 1");
        env.PrimaryDispatcher.Dispatch("sr \"d\" \"id\" 1 0");

        env.Pair.BackupDeviceActive.Should().BeTrue();
        env.Pair.PrimaryDeviceActive.Should().BeFalse();
    }

    [Fact]
    public void Disconnect_clears_active_slot()
    {
        using var env = new PairEnv();

        env.PrimaryDispatcher.Dispatch("sr \"d\" \"id\" 1 1");
        env.Pair.ActiveSlot.Should().Be(CoreSlot.Primary);

        env.Pair.Disconnect();
        env.Pair.ActiveSlot.Should().BeNull();
    }

    [Fact]
    public void Dispose_is_idempotent()
    {
        using var env = new PairEnv();
        env.Pair.Dispose();
        Action again = () => env.Pair.Dispose();
        again.Should().NotThrow();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "PairEnv.Dispose tears down the pair, transports, and queues in order.")]
    private sealed class PairEnv : IDisposable
    {
        public PairEnv()
        {
            const string deviceId = "dsp-1";
            PrimaryTransport = new StubTransport();
            BackupTransport = new StubTransport();
            PrimaryQueue = new EcpCommandQueue(deviceId);
            BackupQueue = new EcpCommandQueue(deviceId);
            PrimaryDispatcher = new EcpDispatcher(deviceId);
            BackupDispatcher = new EcpDispatcher(deviceId);
            RoutingQueue = new EcpRoutingCommandQueue(deviceId);

            var clock = new DeterministicClock();
            var primary = new EcpConnectionManager(
                deviceId,
                PrimaryTransport,
                new ReconnectStrategy(clock),
                PrimaryQueue,
                PrimaryDispatcher,
                credentialsSource: () => null);
            var backup = new EcpConnectionManager(
                deviceId,
                BackupTransport,
                new ReconnectStrategy(clock),
                BackupQueue,
                BackupDispatcher,
                credentialsSource: () => null);

            Pair = new EcpRedundantConnectionPair(deviceId, primary, backup, RoutingQueue, SwitchbackPolicy.Default);
        }

        public StubTransport PrimaryTransport
        {
            get;
        }

        public StubTransport BackupTransport
        {
            get;
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
            Pair.Dispose();
            PrimaryQueue.Dispose();
            BackupQueue.Dispose();
            RoutingQueue.Dispose();
            PrimaryTransport.Dispose();
            BackupTransport.Dispose();
        }
    }
}
