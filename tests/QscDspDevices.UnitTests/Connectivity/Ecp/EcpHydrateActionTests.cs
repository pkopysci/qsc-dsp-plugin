// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.AudioControl;
using QscDspDevices.Connectivity.Ecp;
using QscDspDevices.LogicTriggers;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity.Ecp;

public sealed class EcpHydrateActionTests
{
    private static readonly string[] ExpectedTags =
    {
        "cga 1 \"Out.gain\"",
        "cga 1 \"Out.mute\"",
        "cga 1 \"Router.tag\"",
        "cga 1 \"In.gain\"",
        "cga 1 \"In.mute\"",
        "cga 1 \"Zone.A.enable\"",
        "cga 1 \"Logic.button\"",
    };

    [Fact]
    public void Run_with_no_controls_emits_only_cgc_and_cgsna()
    {
        const string deviceId = "dsp-1";
        var channels = new AudioChannelRegistry(deviceId);
        var zones = new AudioZoneRegistry(deviceId);
        var triggers = new LogicTriggerRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpHydrateAction(deviceId, queue, channels, zones, triggers);

        sut.Run();

        IReadOnlyList<string> commands = queue.SnapshotPending();
        commands.Should().Equal("cgc 1", "cgsna 1 2000");
    }

    [Fact]
    public void Run_emits_cgc_then_cga_per_unique_tag_then_cgsna()
    {
        const string deviceId = "dsp-1";
        var channels = new AudioChannelRegistry(deviceId);
        channels.RegisterOutput(new AudioChannel("out1", "Out.gain", "Out.mute", -100, 0, false, 0, 0, Array.Empty<string>(), RouterTag: "Router.tag"));
        channels.RegisterInput(new AudioChannel("in1", "In.gain", "In.mute", -100, 0, true, 0, 1, Array.Empty<string>()));
        var zones = new AudioZoneRegistry(deviceId);
        zones.TryRegister("in1", "z1", "Zone.A.enable");
        var triggers = new LogicTriggerRegistry(deviceId);
        triggers.Register("trig1", "Logic.button");

        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpHydrateAction(deviceId, queue, channels, zones, triggers);

        sut.Run();

        IReadOnlyList<string> commands = queue.SnapshotPending();
        commands[0].Should().Be("cgc 1");
        commands[^1].Should().Be("cgsna 1 2000");
        commands.Skip(1).Take(commands.Count - 2).Should().Contain(ExpectedTags);
    }

    [Fact]
    public void Run_deduplicates_repeated_tags()
    {
        const string deviceId = "dsp-1";
        var channels = new AudioChannelRegistry(deviceId);

        // Two channels share the same level tag (e.g., a stereo pair).
        channels.RegisterOutput(new AudioChannel("out1", "Common.gain", "Out1.mute", -100, 0, false, 0, 0, Array.Empty<string>()));
        channels.RegisterOutput(new AudioChannel("out2", "Common.gain", "Out2.mute", -100, 0, false, 0, 0, Array.Empty<string>()));
        var zones = new AudioZoneRegistry(deviceId);
        var triggers = new LogicTriggerRegistry(deviceId);

        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpHydrateAction(deviceId, queue, channels, zones, triggers);

        sut.Run();

        IReadOnlyList<string> commands = queue.SnapshotPending();
        commands.Count(c => c == "cga 1 \"Common.gain\"").Should().Be(1);
    }

    [Fact]
    public void Constructor_validates_arguments()
    {
        const string deviceId = "dsp-1";
        var channels = new AudioChannelRegistry(deviceId);
        var zones = new AudioZoneRegistry(deviceId);
        var triggers = new LogicTriggerRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);

        ((Action)(() => { _ = new EcpHydrateAction(null!, queue, channels, zones, triggers); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpHydrateAction(deviceId, null!, channels, zones, triggers); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpHydrateAction(deviceId, queue, null!, zones, triggers); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpHydrateAction(deviceId, queue, channels, null!, triggers); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpHydrateAction(deviceId, queue, channels, zones, null!); })).Should().Throw<ArgumentNullException>();
    }
}
