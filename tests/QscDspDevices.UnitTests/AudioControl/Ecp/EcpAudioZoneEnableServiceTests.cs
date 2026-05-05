// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.AudioControl;
using QscDspDevices.AudioControl.Ecp;
using QscDspDevices.Connectivity.Ecp;
using Xunit;

namespace QscDspDevices.UnitTests.AudioControl.Ecp;

public sealed class EcpAudioZoneEnableServiceTests
{
    [Fact]
    public void Set_emits_css_against_named_zone_control()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioZoneRegistry(deviceId);
        registry.TryRegister("in1", "zone-A", "Zone.A.enable");
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpAudioZoneEnableService(deviceId, registry, queue);

        sut.Set("in1", "zone-A", true);

        queue.SnapshotPending().Should().Equal("css \"Zone.A.enable\" \"true\"");
    }

    [Fact]
    public void Toggle_inverts_cached_state()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioZoneRegistry(deviceId);
        registry.TryRegister("in1", "zone-A", "Zone.A.enable");
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpAudioZoneEnableService(deviceId, registry, queue);

        sut.Toggle("in1", "zone-A");
        sut.Query("in1", "zone-A").Should().BeTrue();

        sut.Toggle("in1", "zone-A");
        sut.Query("in1", "zone-A").Should().BeFalse();

        queue.SnapshotPending().Should().Equal("css \"Zone.A.enable\" \"true\"", "css \"Zone.A.enable\" \"false\"");
    }

    [Fact]
    public void Set_with_unknown_pair_logs_error_and_does_not_enqueue()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioZoneRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpAudioZoneEnableService(deviceId, registry, queue);

        sut.Set("nope", "nope", true);

        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void Query_returns_false_for_unknown_pair()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioZoneRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);
        var sut = new EcpAudioZoneEnableService(deviceId, registry, queue);

        sut.Query("nope", "nope").Should().BeFalse();
    }
}
