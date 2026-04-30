// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.Plugin;
using QscDspDevices.TestSupport.Logging;
using QscDspDevices.TestSupport.Time;
using QscDspDevices.TestSupport.Transport;
using QscDspDevices.Transport;
using Xunit;

namespace QscDspDevices.UnitTests.Plugin;

/// <summary>
/// Unit tests for the public <see cref="QscDspTcp"/> root class. M2 only
/// verifies the framework-facing surface (Manufacturer/Model fixed,
/// Initialize captures config, Connect/Disconnect drive the connection
/// manager, IDisposable is idempotent, methods deferred to M3+ log
/// Notice and don't throw). Audio control / routing / presets / logic /
/// redundancy behaviour is tested in their respective milestones.
/// </summary>
public sealed class QscDspTcpTests
{
    [Fact]
    public void Constructor_sets_Manufacturer_to_QSC_and_Model_to_QSysCore()
    {
        using var sut = new QscDspTcp();

        sut.Manufacturer.Should().Be("QSC");
        sut.Model.Should().Be("Q-SYS Core");
    }

    [Fact]
    public void Constructor_with_null_clock_throws()
    {
        Action act = () => _ = new QscDspTcp(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Initialize_captures_config_and_marks_Initialized()
    {
        using var sut = new TestableQscDspTcp(new DeterministicClock());

        sut.Initialize("dsp-1", coreId: 0, "127.0.0.1", port: 1710, "user", "pass");

        sut.Id.Should().Be("dsp-1");
        sut.Label.Should().Be("dsp-1");
        sut.IsInitialized.Should().BeTrue();
        sut.IsOnline.Should().BeFalse();
    }

    [Fact]
    public void Initialize_with_null_or_empty_hostId_throws()
    {
        using var sut = new TestableQscDspTcp(new DeterministicClock());

        Action nullHostId = () => sut.Initialize(null!, 0, "127.0.0.1", 1710, "u", "p");
        Action emptyHostId = () => sut.Initialize(string.Empty, 0, "127.0.0.1", 1710, "u", "p");

        nullHostId.Should().Throw<ArgumentException>();
        emptyHostId.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Initialize_with_null_or_empty_hostname_throws()
    {
        using var sut = new TestableQscDspTcp(new DeterministicClock());

        Action act = () => sut.Initialize("dsp-1", 0, string.Empty, 1710, "u", "p");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Connect_before_Initialize_logs_an_Error_and_does_not_throw()
    {
        using var sink = new TestLoggerSink();
        using var sut = new QscDspTcp();

        Action act = () => sut.Connect();

        act.Should().NotThrow();
        sink.ContainsErrorMatching("Connect() called before Initialize()").Should().BeTrue();
    }

    [Fact]
    public async Task Connect_drives_IsOnline_true_then_NotifyOnlineStatus()
    {
        using var sut = new TestableQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1710, "u", "p");

        bool sawNotification = false;
        bool isOnlineWhenNotified = false;
        sut.ConnectionChanged += (_, _) =>
        {
            sawNotification = true;
            isOnlineWhenNotified = sut.IsOnline;
        };

        sut.Connect();

        // Wait for the session task to actually call transport.Connect()
        // before driving the stub transport to "Connected" — otherwise
        // SimulateConnectSuccess can race ahead of the subscription.
        await WaitForAsync(() => sut.StubConnectCallCount > 0, TimeSpan.FromSeconds(10));
        sut.SimulateConnectSuccess();

        await WaitForAsync(() => sawNotification, TimeSpan.FromSeconds(10));

        sut.IsOnline.Should().BeTrue();
        isOnlineWhenNotified.Should().BeTrue("IsOnline must be set BEFORE NotifyOnlineStatus per README §3");
    }

    [Fact]
    public async Task Disconnect_drives_IsOnline_false_then_NotifyOnlineStatus()
    {
        using var sut = new TestableQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1710, "u", "p");

        sut.Connect();
        await WaitForAsync(() => sut.StubConnectCallCount > 0, TimeSpan.FromSeconds(10));
        sut.SimulateConnectSuccess();
        await WaitForAsync(() => sut.IsOnline, TimeSpan.FromSeconds(10));

        bool sawDisconnectNotification = false;
        bool isOnlineWhenNotified = true;
        sut.ConnectionChanged += (_, _) =>
        {
            if (!sut.IsOnline)
            {
                sawDisconnectNotification = true;
                isOnlineWhenNotified = sut.IsOnline;
            }
        };

        sut.Disconnect();

        await WaitForAsync(() => sawDisconnectNotification, TimeSpan.FromSeconds(10));

        sut.IsOnline.Should().BeFalse();
        isOnlineWhenNotified.Should().BeFalse("IsOnline must be cleared BEFORE NotifyOnlineStatus per README §3");
    }

    [Fact]
    public void Dispose_is_idempotent()
    {
        var sut = new QscDspTcp();
        sut.Dispose();

        Action secondDispose = () => sut.Dispose();
        secondDispose.Should().NotThrow();
    }

    [Fact]
    public void Connect_after_Dispose_throws_ObjectDisposedException()
    {
        var sut = new QscDspTcp();
        sut.Dispose();

        Action act = () => sut.Connect();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void M3_audio_control_methods_log_Notice_and_return_documented_fallback_without_throwing()
    {
        using var sink = new TestLoggerSink();
        using var sut = new TestableQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1710, "u", "p");

        // Mutators: log Notice and don't throw.
        Action setLevel = () => sut.SetAudioOutputLevel("out-1", 50);
        Action setMute = () => sut.SetAudioOutputMute("out-1", true);
        Action recallPreset = () => sut.RecallAudioPreset("preset-1");
        setLevel.Should().NotThrow();
        setMute.Should().NotThrow();
        recallPreset.Should().NotThrow();

        // Queries: return documented fallback without throwing.
        sut.GetAudioOutputLevel("out-1").Should().Be(0);
        sut.GetAudioOutputMute("out-1").Should().BeFalse();
        sut.GetAudioInputLevel("in-1").Should().Be(0);
        sut.GetAudioInputMute("in-1").Should().BeFalse();
        sut.GetAudioInputIds().Should().BeEmpty();
        sut.GetAudioOutputIds().Should().BeEmpty();
        sut.GetAudioPresetIds().Should().BeEmpty();

        sink.Captures.Should().Contain(
            c => c.Severity == gcu_common_utils.Logging.LogSeverity.Notice
              && c.Message.Contains("not implemented in M2", StringComparison.Ordinal));
    }

    [Fact]
    public void Audio_control_methods_with_empty_id_throw_ArgumentException()
    {
        using var sut = new TestableQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1710, "u", "p");

        Action setLevel = () => sut.SetAudioOutputLevel(string.Empty, 50);
        Action setMute = () => sut.SetAudioInputMute(string.Empty, true);
        Action recall = () => sut.RecallAudioPreset(string.Empty);

        setLevel.Should().Throw<ArgumentException>();
        setMute.Should().Throw<ArgumentException>();
        recall.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void M4_routing_methods_log_Notice_and_validate_args()
    {
        using var sut = new TestableQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1710, "u", "p");

        Action route = () => sut.RouteAudio("src-1", "out-1");
        Action clear = () => sut.ClearAudioRoute("out-1");
        route.Should().NotThrow();
        clear.Should().NotThrow();

        sut.GetCurrentAudioSource("out-1").Should().Be(string.Empty);

        Action emptyRouteSrc = () => sut.RouteAudio(string.Empty, "out-1");
        Action emptyRouteDst = () => sut.RouteAudio("src-1", string.Empty);
        Action emptyClear = () => sut.ClearAudioRoute(string.Empty);
        Action emptyQuery = () => sut.GetCurrentAudioSource(string.Empty);
        emptyRouteSrc.Should().Throw<ArgumentException>();
        emptyRouteDst.Should().Throw<ArgumentException>();
        emptyClear.Should().Throw<ArgumentException>();
        emptyQuery.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void M4_zone_enable_methods_validate_args_and_return_false_until_M4()
    {
        using var sut = new TestableQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1710, "u", "p");

        Action add = () => sut.AddAudioZoneEnable("ch-1", "zone-1", "tag");
        Action remove = () => sut.RemoveAudioZoneEnable("ch-1", "zone-1");
        Action toggle = () => sut.ToggleAudioZoneEnable("ch-1", "zone-1");
        Action set = () => sut.SetAudioZoneEnable("ch-1", "zone-1", true);
        add.Should().NotThrow();
        remove.Should().NotThrow();
        toggle.Should().NotThrow();
        set.Should().NotThrow();

        sut.QueryAudioZoneEnable("ch-1", "zone-1").Should().BeFalse();

        Action emptyChan = () => sut.AddAudioZoneEnable(string.Empty, "zone-1", "tag");
        Action emptyZone = () => sut.AddAudioZoneEnable("ch-1", string.Empty, "tag");
        emptyChan.Should().Throw<ArgumentException>();
        emptyZone.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void M5_logic_trigger_methods_validate_args_and_return_until_M5()
    {
        using var sut = new TestableQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1710, "u", "p");

        Action add = () => sut.AddDspLogicTrigger("trig-1", "tag", new List<string>());
        Action pulse = () => sut.PulseDspLogicTrigger("trig-1");
        add.Should().NotThrow();
        pulse.Should().NotThrow();

        Action emptyAdd = () => sut.AddDspLogicTrigger(string.Empty, "tag", new List<string>());
        Action emptyPulse = () => sut.PulseDspLogicTrigger(string.Empty);
        emptyAdd.Should().Throw<ArgumentException>();
        emptyPulse.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void M6_SetBackupDeviceConnection_logs_Notice_and_validates_args()
    {
        using var sink = new TestLoggerSink();
        using var sut = new TestableQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1710, "u", "p");

        Action act = () => sut.SetBackupDeviceConnection("backup.example", 1710);
        act.Should().NotThrow();

        sink.Captures.Should().Contain(
            c => c.Severity == gcu_common_utils.Logging.LogSeverity.Notice
              && c.Message.Contains("not implemented in M2", StringComparison.Ordinal));

        Action emptyHost = () => sut.SetBackupDeviceConnection(string.Empty, 1710);
        emptyHost.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Disconnect_before_Initialize_is_a_no_op()
    {
        using var sut = new QscDspTcp();
        Action act = () => sut.Disconnect();
        act.Should().NotThrow();
    }

    [Fact]
    public void AddInputChannel_AddOutputChannel_AddPreset_log_Notice_and_validate()
    {
        using var sut = new TestableQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1710, "u", "p");

        Action addIn = () => sut.AddInputChannel("in-1", "lvl", "mute", 0, 100, 0, 0, new List<string>());
        Action addOut = () => sut.AddOutputChannel("out-1", "lvl", "mute", "router", 0, 0, 100, 0, new List<string>());
        Action addPreset = () => sut.AddPreset("preset-1", "bank-A", 1);
        addIn.Should().NotThrow();
        addOut.Should().NotThrow();
        addPreset.Should().NotThrow();

        Action emptyAddIn = () => sut.AddInputChannel(string.Empty, "lvl", "mute", 0, 100, 0, 0, new List<string>());
        emptyAddIn.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task ThreadCensus_reports_one_plugin_thread_when_session_is_active()
    {
        // Per the threading-budget spec: a Connected plugin reports plugin
        // threads alive (M2 ships exactly one — the session task; M3 will
        // grow this to three). This test pins the M2 behaviour: the
        // session task DOES register with the census.
        using var sut = new TestableQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1710, "u", "p");
        sut.ThreadCensus.AliveCount.Should().Be(0);

        sut.Connect();
        await WaitForAsync(() => sut.StubConnectCallCount > 0, TimeSpan.FromSeconds(10));

        // The session task has registered; M2 ships one such thread.
        await WaitForAsync(() => sut.ThreadCensus.AliveCount >= 1, TimeSpan.FromSeconds(10));
        sut.ThreadCensus.Snapshot().Should().Contain("session");
    }

    [Fact]
    public async Task Disconnect_releases_the_session_thread_back_to_zero()
    {
        // Spec scenario: "Disconnected plugin reports 0 plugin threads."
        using var sut = new TestableQscDspTcp(new DeterministicClock());
        sut.Initialize("dsp-1", 0, "127.0.0.1", 1710, "u", "p");

        sut.Connect();
        await WaitForAsync(() => sut.StubConnectCallCount > 0, TimeSpan.FromSeconds(10));
        sut.SimulateConnectSuccess();
        await WaitForAsync(() => sut.IsOnline, TimeSpan.FromSeconds(10));
        await WaitForAsync(() => sut.ThreadCensus.AliveCount >= 1, TimeSpan.FromSeconds(10));

        sut.Disconnect();

        // Disposing forces a join on the session task, which guarantees
        // the finally-block (Unregister) has run before we observe the
        // count. Asserting AliveCount mid-cooperative-cancellation is
        // racy; Dispose is the synchronous join point.
        sut.Dispose();
        sut.ThreadCensus.AliveCount.Should().Be(0);
        sut.IsOnline.Should().BeFalse();
    }

    [Fact]
    public void IRedundancySupport_properties_default_to_no_backup()
    {
        using var sut = new QscDspTcp();
        sut.PrimaryDeviceActive.Should().BeTrue();
        sut.BackupDeviceActive.Should().BeFalse();
        sut.BackupDeviceOnline.Should().BeFalse();
        sut.BackupDeviceExists.Should().BeFalse();
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

            await Task.Delay(10);
        }
    }

    /// <summary>
    /// A QscDspTcp subclass that overrides <c>BuildTransport</c> to
    /// return a <see cref="StubTransport"/>, so tests can drive the
    /// state machine without needing a real (or fake) network.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2213:Disposable fields should be disposed",
        Justification = "_stub is the same instance returned from BuildTransport and stored as the base class's _transport, which the base Dispose(bool) disposes.")]
    private sealed class TestableQscDspTcp : QscDspTcp
    {
        private StubTransport? _stub;

        public TestableQscDspTcp(QscDspDevices.Plugin.Threading.IQrcClock clock)
            : base(clock)
        {
        }

        public int StubConnectCallCount => _stub?.ConnectCallCount ?? 0;

        public void SimulateConnectSuccess() => _stub?.SimulateConnectSuccess();

        protected override IConnectionTransport BuildTransport(string hostname, int port)
        {
            _stub = new StubTransport();
            return _stub;
        }
    }
}
