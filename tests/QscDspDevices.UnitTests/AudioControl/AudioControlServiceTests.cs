// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using QscDspDevices.AudioControl;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.Protocol.JsonRpc;
using Xunit;

namespace QscDspDevices.UnitTests.AudioControl;

/// <summary>
/// Unit tests for <see cref="AudioControlService"/>. Verify that
/// Set* enqueues correct Control.Set requests, Get* serves from the
/// cache, and AutoPoll deltas update the cache and raise the right
/// IAudioControl event.
/// </summary>
public sealed class AudioControlServiceTests
{
    private static readonly IReadOnlyList<string> NoTags = Array.Empty<string>();

    [Fact]
    public void SetLevel_for_known_input_enqueues_Control_Set_with_scaled_Value()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        var sut = new AudioControlService("dsp-1", registry, new LevelScaler("dsp-1"), queue, new IdGenerator());
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 0, NoTags));

        sut.SetLevel("mic1", 50);

        IReadOnlyList<JsonRpcRequest> sent = queue.SnapshotPending();
        sent.Should().HaveCount(1);
        sent[0].Method.Should().Be("Control.Set");
        var p = JObject.FromObject(sent[0].Params!);
        p["Name"]!.ToString().Should().Be("mic1.gain");
        p["Value"]!.ToObject<double>().Should().BeApproximately(-40.0, 1e-9);
    }

    [Fact]
    public void SetMute_for_known_output_enqueues_Control_Set_with_boolean_Value()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        var sut = new AudioControlService("dsp-1", registry, new LevelScaler("dsp-1"), queue, new IdGenerator());
        registry.RegisterOutput(new AudioChannel("out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags));

        sut.SetMute("out1", true);

        IReadOnlyList<JsonRpcRequest> sent = queue.SnapshotPending();
        sent.Should().HaveCount(1);
        var p = JObject.FromObject(sent[0].Params!);
        p["Name"]!.ToString().Should().Be("out1.mute");
        p["Value"]!.ToObject<bool>().Should().BeTrue();
    }

    [Fact]
    public void SetLevel_for_unknown_id_does_not_enqueue_anything()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        var sut = new AudioControlService("dsp-1", registry, new LevelScaler("dsp-1"), queue, new IdGenerator());

        sut.SetLevel("nope", 50);

        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void GetLevel_returns_optimistic_cache_after_SetLevel()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        var sut = new AudioControlService("dsp-1", registry, new LevelScaler("dsp-1"), queue, new IdGenerator());
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 0, NoTags));

        sut.SetLevel("mic1", 75);
        sut.GetLevel("mic1").Should().Be(75);
    }

    [Fact]
    public void GetMute_returns_optimistic_cache_after_SetMute()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        var sut = new AudioControlService("dsp-1", registry, new LevelScaler("dsp-1"), queue, new IdGenerator());
        registry.RegisterOutput(new AudioChannel("o1", "o1.lvl", "o1.mute", 0, 100, false, 0, 0, NoTags));

        sut.SetMute("o1", true);
        sut.GetMute("o1").Should().BeTrue();
    }

    [Fact]
    public void GetLevel_for_unknown_id_returns_zero()
    {
        using CommandQueue queue = NewQueue();
        var sut = new AudioControlService("dsp-1", new AudioChannelRegistry("dsp-1"), new LevelScaler("dsp-1"), queue, new IdGenerator());
        sut.GetLevel("nope").Should().Be(0);
    }

    [Fact]
    public void GetMute_for_unknown_id_returns_false()
    {
        using CommandQueue queue = NewQueue();
        var sut = new AudioControlService("dsp-1", new AudioChannelRegistry("dsp-1"), new LevelScaler("dsp-1"), queue, new IdGenerator());
        sut.GetMute("nope").Should().BeFalse();
    }

    [Fact]
    public void OnDeviceUpdate_with_input_mute_delta_fires_AudioInputMuteChanged()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        var sut = new AudioControlService("dsp-1", registry, new LevelScaler("dsp-1"), queue, new IdGenerator());
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 0, NoTags));

        var raised = new List<string>();
        sut.AudioInputMuteChanged += (_, args) => raised.Add(args.Arg2);

        sut.OnDeviceUpdate(new ChangeGroupDelta("mic1.mute", JToken.FromObject(true), null, null));

        raised.Should().Equal("mic1");
        sut.GetMute("mic1").Should().BeTrue();
    }

    [Fact]
    public void OnDeviceUpdate_with_output_level_position_fires_AudioOutputLevelChanged()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        var sut = new AudioControlService("dsp-1", registry, new LevelScaler("dsp-1"), queue, new IdGenerator());
        registry.RegisterOutput(new AudioChannel("out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags));

        var raised = new List<string>();
        sut.AudioOutputLevelChanged += (_, args) => raised.Add(args.Arg2);

        sut.OnDeviceUpdate(new ChangeGroupDelta("out1.gain", JToken.FromObject(-50.0), null, 0.5));

        raised.Should().Equal("out1");
        sut.GetLevel("out1").Should().Be(50);
    }

    [Fact]
    public void OnDeviceUpdate_no_change_does_not_re_raise_event()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        var sut = new AudioControlService("dsp-1", registry, new LevelScaler("dsp-1"), queue, new IdGenerator());
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 0, NoTags));

        int count = 0;
        sut.AudioInputMuteChanged += (_, _) => count++;

        sut.OnDeviceUpdate(new ChangeGroupDelta("mic1.mute", JToken.FromObject(true), null, null));
        sut.OnDeviceUpdate(new ChangeGroupDelta("mic1.mute", JToken.FromObject(true), null, null));
        sut.OnDeviceUpdate(new ChangeGroupDelta("mic1.mute", JToken.FromObject(true), null, null));

        count.Should().Be(1);
    }

    [Fact]
    public void OnDeviceUpdate_with_unknown_tag_is_silent()
    {
        using CommandQueue queue = NewQueue();
        var sut = new AudioControlService("dsp-1", new AudioChannelRegistry("dsp-1"), new LevelScaler("dsp-1"), queue, new IdGenerator());

        Action act = () => sut.OnDeviceUpdate(new ChangeGroupDelta("never-registered", JToken.FromObject(1), null, null));
        act.Should().NotThrow();
    }

    [Fact]
    public void OnDeviceUpdate_treats_integer_value_as_boolean_for_mute()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        var sut = new AudioControlService("dsp-1", registry, new LevelScaler("dsp-1"), queue, new IdGenerator());
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 0, NoTags));

        bool? observed = null;
        sut.AudioInputMuteChanged += (_, _) => observed = sut.GetMute("mic1");

        sut.OnDeviceUpdate(new ChangeGroupDelta("mic1.mute", JToken.FromObject(1), null, null));

        observed.Should().BeTrue();

        sut.OnDeviceUpdate(new ChangeGroupDelta("mic1.mute", JToken.FromObject(0), null, null));
        sut.GetMute("mic1").Should().BeFalse();
    }

    [Fact]
    public void Constructor_with_null_args_throws()
    {
        using CommandQueue q = NewQueue();
        Action a = () => _ = new AudioControlService(null!, new AudioChannelRegistry("d"), new LevelScaler("d"), q, new IdGenerator());
        Action b = () => _ = new AudioControlService("d", null!, new LevelScaler("d"), q, new IdGenerator());
        Action c = () => _ = new AudioControlService("d", new AudioChannelRegistry("d"), null!, q, new IdGenerator());
        Action e = () => _ = new AudioControlService("d", new AudioChannelRegistry("d"), new LevelScaler("d"), null!, new IdGenerator());
        Action f = () => _ = new AudioControlService("d", new AudioChannelRegistry("d"), new LevelScaler("d"), q, null!);
        a.Should().Throw<ArgumentNullException>();
        b.Should().Throw<ArgumentNullException>();
        c.Should().Throw<ArgumentNullException>();
        e.Should().Throw<ArgumentNullException>();
        f.Should().Throw<ArgumentNullException>();
    }

    private static CommandQueue NewQueue()
    {
        var q = new CommandQueue("dsp-1");
        q.StartAccepting();
        return q;
    }
}
