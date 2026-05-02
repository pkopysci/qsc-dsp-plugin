// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using QscDspDevices.AudioControl;
using QscDspDevices.LogicTriggers;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using Xunit;

namespace QscDspDevices.UnitTests.AudioControl;

/// <summary>
/// Unit tests for <see cref="AudioControlServiceFanout"/>. Pin the
/// dispatch precedence: routerTag → routing, zoneTag → zone, otherwise
/// → audio control.
/// </summary>
public sealed class AudioControlServiceFanoutTests
{
    private static readonly IReadOnlyList<string> NoTags = Array.Empty<string>();

    [Fact]
    public void Router_tag_dispatches_to_routing_service()
    {
        using CommandQueue queue = NewQueue();
        FanoutTestEnv env = BuildServices(queue);
        AudioChannelRegistry channels = env.Channels;
        AudioZoneRegistry zones = env.Zones;
        AudioRoutingService routing = env.Routing;
        AudioZoneEnableService zone = env.Zone;
        AudioControlService audio = env.Audio;
        channels.RegisterOutput(new AudioChannel(
            "out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, "mixer.out1.source"));
        channels.RegisterInput(new AudioChannel(
            "mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 5, NoTags));

        var sut = new AudioControlServiceFanout(channels, zones, routing, zone, audio);

        bool routeFired = false;
        routing.RouteChanged += (_, _) => routeFired = true;

        sut.Dispatch(new ChangeGroupDelta("mixer.out1.source", JToken.FromObject(5), null, null));

        routeFired.Should().BeTrue();
    }

    [Fact]
    public void Zone_tag_dispatches_to_zone_service()
    {
        using CommandQueue queue = NewQueue();
        FanoutTestEnv env = BuildServices(queue);
        AudioChannelRegistry channels = env.Channels;
        AudioZoneRegistry zones = env.Zones;
        AudioRoutingService routing = env.Routing;
        AudioZoneEnableService zone = env.Zone;
        AudioControlService audio = env.Audio;
        zones.TryRegister("mic1", "zoneA", "zone.tag.A");

        var sut = new AudioControlServiceFanout(channels, zones, routing, zone, audio);

        bool zoneFired = false;
        zone.ZoneEnableChanged += (_, _) => zoneFired = true;

        sut.Dispatch(new ChangeGroupDelta("zone.tag.A", JToken.FromObject(true), null, null));

        zoneFired.Should().BeTrue();
    }

    [Fact]
    public void Other_tag_dispatches_to_audio_service()
    {
        using CommandQueue queue = NewQueue();
        FanoutTestEnv env = BuildServices(queue);
        AudioChannelRegistry channels = env.Channels;
        AudioZoneRegistry zones = env.Zones;
        AudioRoutingService routing = env.Routing;
        AudioZoneEnableService zone = env.Zone;
        AudioControlService audio = env.Audio;
        channels.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 0, NoTags));

        var sut = new AudioControlServiceFanout(channels, zones, routing, zone, audio);

        bool muteFired = false;
        audio.AudioInputMuteChanged += (_, _) => muteFired = true;

        sut.Dispatch(new ChangeGroupDelta("mic1.mute", JToken.FromObject(true), null, null));

        muteFired.Should().BeTrue();
    }

    [Fact]
    public void Trigger_tag_dispatches_to_trigger_service_when_registry_is_supplied()
    {
        using CommandQueue queue = NewQueue();
        var channels = new AudioChannelRegistry("dsp-1");
        var zones = new AudioZoneRegistry("dsp-1");
        var triggers = new LogicTriggerRegistry("dsp-1");
        triggers.Register("rec", "rec.start");

        var ids = new IdGenerator();
        var routing = new AudioRoutingService("dsp-1", channels, queue, ids);
        var zone = new AudioZoneEnableService("dsp-1", zones, queue, ids);
        var trigger = new LogicTriggerService("dsp-1", triggers, queue, ids);
        var audio = new AudioControlService("dsp-1", channels, new LevelScaler("dsp-1"), queue, ids);

        var sut = new AudioControlServiceFanout(channels, zones, triggers, routing, zone, trigger, audio);

        bool triggerFired = false;
        trigger.LogicTriggerStateChanged += (_, _) => triggerFired = true;

        sut.Dispatch(new ChangeGroupDelta("rec.start", JToken.FromObject(true), null, null));

        triggerFired.Should().BeTrue();
    }

    [Fact]
    public void Tag_registered_as_both_trigger_and_audio_level_dispatches_to_trigger()
    {
        // Designer-side configuration error: the precedence is
        // router → zone → trigger → audio, so a tag claimed by both
        // a trigger and a channel's levelTag fires the trigger event,
        // not the audio level event.
        using CommandQueue queue = NewQueue();
        var channels = new AudioChannelRegistry("dsp-1");
        var zones = new AudioZoneRegistry("dsp-1");
        var triggers = new LogicTriggerRegistry("dsp-1");
        channels.RegisterInput(new AudioChannel(
            "mic1", "shared.tag", "mic1.mute", -80, 0, true, 0, 1, NoTags));
        triggers.Register("rec", "shared.tag");

        var ids = new IdGenerator();
        var routing = new AudioRoutingService("dsp-1", channels, queue, ids);
        var zone = new AudioZoneEnableService("dsp-1", zones, queue, ids);
        var trigger = new LogicTriggerService("dsp-1", triggers, queue, ids);
        var audio = new AudioControlService("dsp-1", channels, new LevelScaler("dsp-1"), queue, ids);

        var sut = new AudioControlServiceFanout(channels, zones, triggers, routing, zone, trigger, audio);

        bool triggerFired = false;
        bool audioFired = false;
        trigger.LogicTriggerStateChanged += (_, _) => triggerFired = true;
        audio.AudioInputLevelChanged += (_, _) => audioFired = true;

        sut.Dispatch(new ChangeGroupDelta("shared.tag", JToken.FromObject(true), null, null));

        triggerFired.Should().BeTrue();
        audioFired.Should().BeFalse();
    }

    [Fact]
    public void Tag_registered_in_both_router_and_zone_dispatches_to_routing_first()
    {
        // Designer-side configuration error: same tag registered as
        // both a routerTag and a zone-enable controlTag. We document
        // the precedence order and pin it in case it ever changes.
        using CommandQueue queue = NewQueue();
        FanoutTestEnv env = BuildServices(queue);
        AudioChannelRegistry channels = env.Channels;
        AudioZoneRegistry zones = env.Zones;
        AudioRoutingService routing = env.Routing;
        AudioZoneEnableService zone = env.Zone;
        AudioControlService audio = env.Audio;
        channels.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 5, NoTags));
        channels.RegisterOutput(new AudioChannel(
            "out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, "shared.tag"));
        zones.TryRegister("mic1", "zoneA", "shared.tag");

        var sut = new AudioControlServiceFanout(channels, zones, routing, zone, audio);

        bool routeFired = false;
        bool zoneFired = false;
        routing.RouteChanged += (_, _) => routeFired = true;
        zone.ZoneEnableChanged += (_, _) => zoneFired = true;

        sut.Dispatch(new ChangeGroupDelta("shared.tag", JToken.FromObject(5), null, null));

        routeFired.Should().BeTrue();
        zoneFired.Should().BeFalse();
    }

    [Fact]
    public void Constructor_with_null_args_throws()
    {
        using CommandQueue queue = NewQueue();
        FanoutTestEnv env = BuildServices(queue);
        AudioChannelRegistry channels = env.Channels;
        AudioZoneRegistry zones = env.Zones;
        AudioRoutingService routing = env.Routing;
        AudioZoneEnableService zone = env.Zone;
        AudioControlService audio = env.Audio;

        Action a = () => _ = new AudioControlServiceFanout(null!, zones, routing, zone, audio);
        Action b = () => _ = new AudioControlServiceFanout(channels, null!, routing, zone, audio);
        Action c = () => _ = new AudioControlServiceFanout(channels, zones, null!, zone, audio);
        Action e = () => _ = new AudioControlServiceFanout(channels, zones, routing, null!, audio);
        Action f = () => _ = new AudioControlServiceFanout(channels, zones, routing, zone, null!);

        a.Should().Throw<ArgumentNullException>();
        b.Should().Throw<ArgumentNullException>();
        c.Should().Throw<ArgumentNullException>();
        e.Should().Throw<ArgumentNullException>();
        f.Should().Throw<ArgumentNullException>();
    }

    private static FanoutTestEnv BuildServices(CommandQueue queue)
    {
        var channels = new AudioChannelRegistry("dsp-1");
        var zones = new AudioZoneRegistry("dsp-1");
        var ids = new IdGenerator();
        var routing = new AudioRoutingService("dsp-1", channels, queue, ids);
        var zone = new AudioZoneEnableService("dsp-1", zones, queue, ids);
        var audio = new AudioControlService("dsp-1", channels, new LevelScaler("dsp-1"), queue, ids);
        return new FanoutTestEnv(channels, zones, routing, zone, audio);
    }

    private static CommandQueue NewQueue()
    {
        var q = new CommandQueue("dsp-1");
        q.StartAccepting();
        return q;
    }

    private sealed record FanoutTestEnv(
        AudioChannelRegistry Channels,
        AudioZoneRegistry Zones,
        AudioRoutingService Routing,
        AudioZoneEnableService Zone,
        AudioControlService Audio);
}
