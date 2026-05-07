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
/// Unit tests for <see cref="AudioRoutingService"/>. Pin: Route enqueues
/// Control.Set with bankIndex; Clear sends 0; Get serves cache; AutoPoll
/// translates bankIndex back to channelId.
/// </summary>
public sealed class AudioRoutingServiceTests
{
    private static readonly IReadOnlyList<string> NoTags = Array.Empty<string>();

    [Fact]
    public void Route_enqueues_Control_Set_with_source_bank_index_on_routerTag()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 3, 0, NoTags));
        registry.RegisterOutput(new AudioChannel("out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, "mixer.out1.source"));
        var sut = new AudioRoutingService("dsp-1", registry, queue, new IdGenerator());

        sut.Route("mic1", "out1");

        IReadOnlyList<JsonRpcRequest> sent = queue.SnapshotPending();
        sent.Should().HaveCount(1);
        sent[0].Method.Should().Be("Control.Set");
        var p = JObject.FromObject(sent[0].Params!);
        p["Name"]!.ToString().Should().Be("mixer.out1.source");
        p["Value"]!.ToObject<int>().Should().Be(3);
    }

    [Fact]
    public void Route_updates_cache_optimistically_so_GetCurrentSource_returns_immediately()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 3, 0, NoTags));
        registry.RegisterOutput(new AudioChannel("out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, "mixer.out1.source"));
        var sut = new AudioRoutingService("dsp-1", registry, queue, new IdGenerator());

        sut.Route("mic1", "out1");

        sut.GetCurrentSource("out1").Should().Be("mic1");
    }

    [Fact]
    public void Clear_sends_Control_Set_with_value_zero_and_empties_the_cache()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 3, 0, NoTags));
        registry.RegisterOutput(new AudioChannel("out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, "mixer.out1.source"));
        var sut = new AudioRoutingService("dsp-1", registry, queue, new IdGenerator());

        sut.Route("mic1", "out1");
        sut.Clear("out1");

        IReadOnlyList<JsonRpcRequest> sent = queue.SnapshotPending();
        sent.Should().HaveCount(2);
        var clearParams = JObject.FromObject(sent[1].Params!);
        clearParams["Value"]!.ToObject<int>().Should().Be(AudioRoutingService.ClearedSourceValue);
        sut.GetCurrentSource("out1").Should().Be(string.Empty);
    }

    [Fact]
    public void GetCurrentSource_unknown_output_returns_empty_string()
    {
        using CommandQueue queue = NewQueue();
        var sut = new AudioRoutingService("dsp-1", new AudioChannelRegistry("dsp-1"), queue, new IdGenerator());
        sut.GetCurrentSource("nope").Should().Be(string.Empty);
    }

    [Fact]
    public void Route_unknown_output_logs_error_and_does_not_enqueue()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 3, 0, NoTags));
        var sut = new AudioRoutingService("dsp-1", registry, queue, new IdGenerator());

        sut.Route("mic1", "nope");

        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void Route_unknown_source_logs_error_and_does_not_enqueue()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        registry.RegisterOutput(new AudioChannel("out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, "mixer.out1.source"));
        var sut = new AudioRoutingService("dsp-1", registry, queue, new IdGenerator());

        sut.Route("nope", "out1");

        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void Route_to_output_without_routerTag_logs_error_and_does_not_enqueue()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 3, 0, NoTags));
        registry.RegisterOutput(new AudioChannel("out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, string.Empty));
        var sut = new AudioRoutingService("dsp-1", registry, queue, new IdGenerator());

        sut.Route("mic1", "out1");

        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void OnDeviceUpdate_with_known_bank_index_fires_RouteChanged_with_resolved_channelId()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 3, 0, NoTags));
        registry.RegisterInput(new AudioChannel("mic2", "mic2.gain", "mic2.mute", -80, 0, true, 5, 0, NoTags));
        registry.RegisterOutput(new AudioChannel("out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, "mixer.out1.source"));
        var sut = new AudioRoutingService("dsp-1", registry, queue, new IdGenerator());

        var raised = new List<string>();
        sut.RouteChanged += (_, args) => raised.Add(args.Arg2);

        sut.OnDeviceUpdate(new ChangeGroupDelta("mixer.out1.source", JToken.FromObject(5), null, null));

        raised.Should().Equal("out1");
        sut.GetCurrentSource("out1").Should().Be("mic2");
    }

    [Fact]
    public void OnDeviceUpdate_with_value_zero_clears_cache_to_empty()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 3, 0, NoTags));
        registry.RegisterOutput(new AudioChannel("out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, "mixer.out1.source"));
        var sut = new AudioRoutingService("dsp-1", registry, queue, new IdGenerator());
        sut.Route("mic1", "out1"); // populates cache

        sut.OnDeviceUpdate(new ChangeGroupDelta("mixer.out1.source", JToken.FromObject(0), null, null));

        sut.GetCurrentSource("out1").Should().Be(string.Empty);
    }

    [Fact]
    public void OnDeviceUpdate_with_unknown_bank_index_clears_cache_to_empty()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 3, 0, NoTags));
        registry.RegisterOutput(new AudioChannel("out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, "mixer.out1.source"));
        var sut = new AudioRoutingService("dsp-1", registry, queue, new IdGenerator());

        sut.OnDeviceUpdate(new ChangeGroupDelta("mixer.out1.source", JToken.FromObject(99), null, null));

        sut.GetCurrentSource("out1").Should().Be(string.Empty);
    }

    [Fact]
    public void Clear_on_a_never_routed_output_does_not_fire_RouteChanged()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        registry.RegisterOutput(new AudioChannel("out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, "mixer.out1.source"));
        var sut = new AudioRoutingService("dsp-1", registry, queue, new IdGenerator());

        bool fired = false;
        sut.RouteChanged += (_, _) => fired = true;

        sut.Clear("out1");

        // The Control.Set with Value=0 IS sent (idempotent on the wire,
        // safe-by-default for an unknown server-side state) but the
        // empty→empty transition does NOT fire the framework event.
        fired.Should().BeFalse();
        sut.GetCurrentSource("out1").Should().Be(string.Empty);
        queue.SnapshotPending().Should().HaveCount(1);
    }

    [Fact]
    public void Route_rejects_a_source_with_invalid_bank_index_zero()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");

        // bankIndex=0 is the QSC "no source" sentinel — routing this
        // input would clear the output instead, contradicting the cache.
        registry.RegisterInput(new AudioChannel("badmic", "lvl", "mute", -80, 0, true, 0, 0, NoTags));
        registry.RegisterOutput(new AudioChannel("out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, "mixer.out1.source"));
        var sut = new AudioRoutingService("dsp-1", registry, queue, new IdGenerator());

        sut.Route("badmic", "out1");

        queue.SnapshotPending().Should().BeEmpty();
        sut.GetCurrentSource("out1").Should().Be(string.Empty);
    }

    [Fact]
    public void Route_rejects_a_source_with_negative_bank_index()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        registry.RegisterInput(new AudioChannel("badmic", "lvl", "mute", -80, 0, true, 0, -1, NoTags));
        registry.RegisterOutput(new AudioChannel("out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, "mixer.out1.source"));
        var sut = new AudioRoutingService("dsp-1", registry, queue, new IdGenerator());

        sut.Route("badmic", "out1");

        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void Constructor_with_null_args_throws()
    {
        using CommandQueue q = NewQueue();
        Action a = () => _ = new AudioRoutingService(null!, new AudioChannelRegistry("d"), q, new IdGenerator());
        Action b = () => _ = new AudioRoutingService("d", null!, q, new IdGenerator());
        Action c = () => _ = new AudioRoutingService("d", new AudioChannelRegistry("d"), null!, new IdGenerator());
        Action e = () => _ = new AudioRoutingService("d", new AudioChannelRegistry("d"), q, null!);
        a.Should().Throw<ArgumentNullException>();
        b.Should().Throw<ArgumentNullException>();
        c.Should().Throw<ArgumentNullException>();
        e.Should().Throw<ArgumentNullException>();
    }

    private static CommandQueue NewQueue()
    {
        var q = new CommandQueue("dsp-1");
        q.StartAccepting();
        return q;
    }
}
