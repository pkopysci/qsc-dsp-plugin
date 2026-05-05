// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.TestSupport.Time;
using QscDspDevices.TestSupport.Transport;
using QscDspDevices.Transport;
using Xunit;

namespace QscDspDevices.UnitTests.Plugin;

/// <summary>
/// Unit tests for the M-ECP-part-2 protocol-by-port branching in
/// <see cref="QscDspTcp"/>. Drives the public surface with a stubbed
/// transport so the ECP service-tier wiring is exercised without
/// needing a real or fake TCP server.
/// </summary>
public sealed class QscDspTcpEcpTests
{
    [Fact]
    public void Initialize_with_port_1702_selects_ECP_backend_and_routes_set_to_ecp_service()
    {
        using var sut = new TestableEcpQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", coreId: 0, "127.0.0.1", port: 1702, username: string.Empty, password: string.Empty);
        sut.AddOutputChannel("out1", "Output.gain", "Output.mute", routerTag: string.Empty, routerIndex: 0, bankIndex: 0, levelMax: 0, levelMin: -100, tags: new List<string>());

        // Set fires on the ECP service which uses the ECP queue.
        sut.SetAudioOutputLevel("out1", 50);
        sut.SetAudioOutputMute("out1", true);

        // The ECP queue refuses while not Accepting (we never called
        // Connect). The ECP service still updates the optimistic cache.
        sut.GetAudioOutputLevel("out1").Should().Be(50);
        sut.GetAudioOutputMute("out1").Should().BeTrue();
    }

    [Fact]
    public void Initialize_with_port_1710_selects_QRC_backend_unchanged()
    {
        using var sut = new TestableEcpQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", coreId: 0, "127.0.0.1", port: 1710, username: string.Empty, password: string.Empty);

        // The QRC AudioControlService is wired; its SetLevel runs.
        // No exception, no ECP path engaged.
        Action act = () =>
        {
            sut.AddOutputChannel("out1", "Output.gain", "Output.mute", string.Empty, 0, 0, 0, -100, new List<string>());
            sut.SetAudioOutputLevel("out1", 50);
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void Initialize_with_non_standard_port_falls_back_to_QRC()
    {
        using var sut = new TestableEcpQscDspTcp(new DeterministicClock());
        Action act = () => sut.Initialize("dsp-1", 0, "127.0.0.1", port: 9999, string.Empty, string.Empty);
        act.Should().NotThrow();
        sut.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void SetBackupDeviceConnection_with_mixed_protocol_pair_is_refused()
    {
        using var sut = new TestableEcpQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1710, string.Empty, string.Empty);

        sut.SetBackupDeviceConnection("127.0.0.2", 1702);
        sut.BackupDeviceExists.Should().BeFalse();
    }

    [Fact]
    public void SetBackupDeviceConnection_same_protocol_ECP_pair_is_refused_pending_part_3()
    {
        using var sut = new TestableEcpQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1702, string.Empty, string.Empty);

        sut.SetBackupDeviceConnection("127.0.0.2", 1702);
        sut.BackupDeviceExists.Should().BeFalse();
    }

    [Fact]
    public void Disconnect_on_ECP_path_does_not_throw_before_Connect()
    {
        using var sut = new TestableEcpQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1702, string.Empty, string.Empty);

        Action act = sut.Disconnect;
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Connect_on_ECP_path_drives_OnEcpStateChanged_to_IsOnline_true()
    {
        using var sut = new TestableEcpQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1702, string.Empty, string.Empty);

        sut.Connect();
        await Task.Delay(200);
        sut.SimulateConnectSuccess();

        await WaitForAsync(() => sut.IsOnline, TimeSpan.FromSeconds(15));

        sut.Disconnect();
        await WaitForAsync(() => !sut.IsOnline, TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void RouteAudio_on_ECP_path_does_not_throw_for_known_channels()
    {
        using var sut = new TestableEcpQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1702, string.Empty, string.Empty);
        sut.AddInputChannel("in1", "L", "M", bankIndex: 3, levelMax: 0, levelMin: -100, routerIndex: 0, new List<string>());
        sut.AddOutputChannel("out1", "L", "M", "Router.tag", 0, 0, 0, -100, new List<string>());

        Action act = () => sut.RouteAudio("in1", "out1");
        act.Should().NotThrow();
    }

    [Fact]
    public void Pulse_zone_and_query_on_ECP_path_do_not_throw()
    {
        using var sut = new TestableEcpQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1702, string.Empty, string.Empty);
        sut.AddDspLogicTrigger("trig1", "Logic.button", new List<string>());
        sut.AddAudioZoneEnable("in1", "z1", "Zone.enable");

        Action pulse = () => sut.PulseDspLogicTrigger("trig1");
        Action toggle = () => sut.ToggleAudioZoneEnable("in1", "z1");
        Action set = () => sut.SetAudioZoneEnable("in1", "z1", true);
        Action query = () => sut.QueryAudioZoneEnable("in1", "z1");

        pulse.Should().NotThrow();
        toggle.Should().NotThrow();
        set.Should().NotThrow();
        query.Should().NotThrow();
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2213:Disposable fields should be disposed",
        Justification = "_stub is the same instance returned from BuildTransport and stored as the base class's _transport, which the base Dispose(bool) disposes.")]
    private sealed class TestableEcpQscDspTcp : QscDspTcp
    {
        private StubTransport? _stub;

        public TestableEcpQscDspTcp(IQrcClock clock)
            : base(clock)
        {
        }

        public void SimulateConnectSuccess() => _stub?.SimulateConnectSuccess();

        protected override IConnectionTransport BuildTransport(string hostname, int port)
        {
            _stub = new StubTransport();
            return _stub;
        }
    }
}
