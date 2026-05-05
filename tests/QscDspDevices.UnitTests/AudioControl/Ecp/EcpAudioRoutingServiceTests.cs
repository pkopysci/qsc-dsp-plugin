// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.AudioControl;
using QscDspDevices.AudioControl.Ecp;
using QscDspDevices.Connectivity.Ecp;
using Xunit;

namespace QscDspDevices.UnitTests.AudioControl.Ecp;

public sealed class EcpAudioRoutingServiceTests
{
    [Fact]
    public void Route_emits_csv_against_router_tag_with_source_bank_index()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioChannelRegistry(deviceId);
        registry.RegisterInput(new AudioChannel("in1", "L", "M", -100, 0, true, 0, BankIndex: 3, Array.Empty<string>()));
        registry.RegisterOutput(new AudioChannel("out1", "L", "M", -100, 0, false, 0, 0, Array.Empty<string>(), RouterTag: "Mixer.input.1.gain"));
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpAudioRoutingService(deviceId, registry, queue);

        sut.Route("in1", "out1");

        IReadOnlyList<string> commands = queue.SnapshotPending();
        commands.Should().Equal("csv \"Mixer.input.1.gain\" 3");
    }

    [Fact]
    public void Clear_emits_csv_zero_against_router_tag()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioChannelRegistry(deviceId);
        registry.RegisterOutput(new AudioChannel("out1", "L", "M", -100, 0, false, 0, 0, Array.Empty<string>(), RouterTag: "Mixer.input.1.gain"));
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpAudioRoutingService(deviceId, registry, queue);

        sut.Clear("out1");

        queue.SnapshotPending().Should().Equal("csv \"Mixer.input.1.gain\" 0");
    }

    [Fact]
    public void Query_returns_empty_for_unrouted_output()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioChannelRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);
        var sut = new EcpAudioRoutingService(deviceId, registry, queue);

        sut.Query("nope").Should().BeEmpty();
    }

    [Fact]
    public void Route_with_unknown_output_logs_error_and_does_not_enqueue()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioChannelRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpAudioRoutingService(deviceId, registry, queue);

        sut.Route("in1", "missing");

        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void Route_with_unknown_source_logs_error_and_does_not_enqueue()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioChannelRegistry(deviceId);
        registry.RegisterOutput(new AudioChannel("out1", "L", "M", -100, 0, false, 0, 0, Array.Empty<string>(), RouterTag: "tag"));
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpAudioRoutingService(deviceId, registry, queue);

        sut.Route("missing", "out1");

        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void Route_with_zero_bankIndex_source_logs_error_and_does_not_enqueue()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioChannelRegistry(deviceId);
        registry.RegisterInput(new AudioChannel("in1", "L", "M", -100, 0, true, 0, BankIndex: 0, Array.Empty<string>()));
        registry.RegisterOutput(new AudioChannel("out1", "L", "M", -100, 0, false, 0, 0, Array.Empty<string>(), RouterTag: "tag"));
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpAudioRoutingService(deviceId, registry, queue);

        sut.Route("in1", "out1");

        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void Route_then_Query_returns_routed_source()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioChannelRegistry(deviceId);
        registry.RegisterInput(new AudioChannel("in1", "L", "M", -100, 0, true, 0, BankIndex: 3, Array.Empty<string>()));
        registry.RegisterOutput(new AudioChannel("out1", "L", "M", -100, 0, false, 0, 0, Array.Empty<string>(), RouterTag: "tag"));
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpAudioRoutingService(deviceId, registry, queue);

        sut.Route("in1", "out1");
        sut.Query("out1").Should().Be("in1");

        sut.Clear("out1");
        sut.Query("out1").Should().BeEmpty();
    }
}
