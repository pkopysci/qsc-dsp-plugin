// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.AudioControl;
using QscDspDevices.AudioControl.Ecp;
using QscDspDevices.Connectivity.Ecp;
using Xunit;

namespace QscDspDevices.UnitTests.AudioControl.Ecp;

public sealed class EcpAudioControlServiceTests
{
    [Fact]
    public void SetLevel_emits_csv_against_the_named_level_tag()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioChannelRegistry(deviceId);
        registry.RegisterOutput(new AudioChannel("out1", "Output.gain", "Output.mute", LevelMin: -100, LevelMax: 0, IsInput: false, RouterIndex: 0, BankIndex: 0, Tags: Array.Empty<string>(), RouterTag: string.Empty));
        var scaler = new LevelScaler(deviceId);
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpAudioControlService(deviceId, registry, scaler, queue);

        sut.SetLevel("out1", 50);

        IReadOnlyList<string> commands = queue.SnapshotPending();
        commands.Should().HaveCount(1);
        commands[0].Should().StartWith("csv \"Output.gain\" ");
    }

    [Fact]
    public void SetMute_emits_css_with_true_or_false()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioChannelRegistry(deviceId);
        registry.RegisterOutput(new AudioChannel("out1", "Output.gain", "Output.mute", -100, 0, false, 0, 0, Array.Empty<string>(), string.Empty));
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpAudioControlService(deviceId, registry, new LevelScaler(deviceId), queue);

        sut.SetMute("out1", true);
        sut.SetMute("out1", false);

        IReadOnlyList<string> commands = queue.SnapshotPending();
        commands.Should().Equal("css \"Output.mute\" \"true\"", "css \"Output.mute\" \"false\"");
    }

    [Fact]
    public void GetLevel_returns_zero_for_unknown_channel()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioChannelRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);
        var sut = new EcpAudioControlService(deviceId, registry, new LevelScaler(deviceId), queue);

        sut.GetLevel("nope").Should().Be(0);
        sut.GetMute("nope").Should().BeFalse();
    }

    [Fact]
    public void Set_with_unknown_channel_logs_error_and_does_not_enqueue()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioChannelRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpAudioControlService(deviceId, registry, new LevelScaler(deviceId), queue);

        sut.SetLevel("nope", 50);
        sut.SetMute("nope", true);
        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void RecallPreset_emits_ssl_for_known_preset()
    {
        const string deviceId = "dsp-1";
        var registry = new AudioChannelRegistry(deviceId);
        registry.RegisterPreset(new AudioPreset("p1", "MainBank", 3));
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpAudioControlService(deviceId, registry, new LevelScaler(deviceId), queue);

        sut.RecallPreset("p1");

        queue.SnapshotPending().Should().Equal("ssl \"MainBank\" 3 0");
    }
}
