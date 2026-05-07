// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.AudioControl;
using QscDspDevices.AudioControl.Ecp;
using QscDspDevices.Connectivity.Ecp;
using QscDspDevices.LogicTriggers;
using QscDspDevices.LogicTriggers.Ecp;
using QscDspDevices.Protocol.Ecp;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity.Ecp;

public sealed class EcpAutoPollSubscriptionTests
{
    [Fact]
    public void Inbound_cv_for_level_tag_updates_audio_cache_and_raises_event()
    {
        const string deviceId = "dsp-1";
        var channels = new AudioChannelRegistry(deviceId);
        channels.RegisterOutput(new AudioChannel("out1", "Out.gain", "Out.mute", LevelMin: -100, LevelMax: 0, IsInput: false, RouterIndex: 0, BankIndex: 0, Tags: Array.Empty<string>()));
        var zones = new AudioZoneRegistry(deviceId);
        var triggers = new LogicTriggerRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);
        var dispatcher = new EcpDispatcher(deviceId);
        var audio = new EcpAudioControlService(deviceId, channels, new LevelScaler(deviceId), queue);
        var routing = new EcpAudioRoutingService(deviceId, channels, queue);
        var zoneEnable = new EcpAudioZoneEnableService(deviceId, zones, queue);
        var logic = new EcpLogicTriggerService(deviceId, triggers, queue);

        using var sut = new EcpAutoPollSubscription(
            deviceId,
            dispatcher,
            channels,
            zones,
            triggers,
            audio,
            routing,
            zoneEnable,
            logic);

        string? capturedChannel = null;
        audio.AudioOutputLevelChanged += (_, args) => capturedChannel = args.Arg1;

        // Core says Out.gain = -50 (out of [-100, 0] → ~50% framework).
        dispatcher.Dispatch("cv \"Out.gain\" \"-50dB\" -50 0.5");

        capturedChannel.Should().Be("out1");
        audio.GetLevel("out1").Should().BeInRange(49, 51);
    }

    [Fact]
    public void Inbound_cv_for_mute_tag_updates_mute_cache()
    {
        const string deviceId = "dsp-1";
        var channels = new AudioChannelRegistry(deviceId);
        channels.RegisterInput(new AudioChannel("in1", "In.gain", "In.mute", -100, 0, true, 1, 0, Array.Empty<string>()));
        var zones = new AudioZoneRegistry(deviceId);
        var triggers = new LogicTriggerRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);
        var dispatcher = new EcpDispatcher(deviceId);
        var audio = new EcpAudioControlService(deviceId, channels, new LevelScaler(deviceId), queue);
        using var sut = new EcpAutoPollSubscription(
            deviceId,
            dispatcher,
            channels,
            zones,
            triggers,
            audio,
            new EcpAudioRoutingService(deviceId, channels, queue),
            new EcpAudioZoneEnableService(deviceId, zones, queue),
            new EcpLogicTriggerService(deviceId, triggers, queue));

        dispatcher.Dispatch("cv \"In.mute\" \"true\" 1 0");

        audio.GetMute("in1").Should().BeTrue();
    }

    [Fact]
    public void Inbound_cv_for_zone_tag_updates_zone_cache()
    {
        const string deviceId = "dsp-1";
        var channels = new AudioChannelRegistry(deviceId);
        var zones = new AudioZoneRegistry(deviceId);
        zones.TryRegister("in1", "z1", "Zone.enable");
        var triggers = new LogicTriggerRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);
        var dispatcher = new EcpDispatcher(deviceId);
        var zoneEnable = new EcpAudioZoneEnableService(deviceId, zones, queue);
        using var sut = new EcpAutoPollSubscription(
            deviceId,
            dispatcher,
            channels,
            zones,
            triggers,
            new EcpAudioControlService(deviceId, channels, new LevelScaler(deviceId), queue),
            new EcpAudioRoutingService(deviceId, channels, queue),
            zoneEnable,
            new EcpLogicTriggerService(deviceId, triggers, queue));

        dispatcher.Dispatch("cv \"Zone.enable\" \"true\" 1 0");

        zoneEnable.Query("in1", "z1").Should().BeTrue();
    }

    [Fact]
    public void Inbound_cv_for_unknown_tag_logs_warn_and_drops()
    {
        const string deviceId = "dsp-1";
        var channels = new AudioChannelRegistry(deviceId);
        var zones = new AudioZoneRegistry(deviceId);
        var triggers = new LogicTriggerRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);
        var dispatcher = new EcpDispatcher(deviceId);
        using var sut = new EcpAutoPollSubscription(
            deviceId,
            dispatcher,
            channels,
            zones,
            triggers,
            new EcpAudioControlService(deviceId, channels, new LevelScaler(deviceId), queue),
            new EcpAudioRoutingService(deviceId, channels, queue),
            new EcpAudioZoneEnableService(deviceId, zones, queue),
            new EcpLogicTriggerService(deviceId, triggers, queue));

        Action act = () => dispatcher.Dispatch("cv \"unknown.tag\" \"-50\" -50 0.5");
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_unsubscribes_from_dispatcher()
    {
        const string deviceId = "dsp-1";
        var channels = new AudioChannelRegistry(deviceId);
        channels.RegisterOutput(new AudioChannel("out1", "Out.gain", "Out.mute", -100, 0, false, 0, 0, Array.Empty<string>()));
        var zones = new AudioZoneRegistry(deviceId);
        var triggers = new LogicTriggerRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);
        var dispatcher = new EcpDispatcher(deviceId);
        var audio = new EcpAudioControlService(deviceId, channels, new LevelScaler(deviceId), queue);
        var sut = new EcpAutoPollSubscription(
            deviceId,
            dispatcher,
            channels,
            zones,
            triggers,
            audio,
            new EcpAudioRoutingService(deviceId, channels, queue),
            new EcpAudioZoneEnableService(deviceId, zones, queue),
            new EcpLogicTriggerService(deviceId, triggers, queue));

        sut.Dispose();

        int events = 0;
        audio.AudioOutputLevelChanged += (_, _) => events++;
        dispatcher.Dispatch("cv \"Out.gain\" \"-50\" -50 0.5");
        events.Should().Be(0);
    }
}
