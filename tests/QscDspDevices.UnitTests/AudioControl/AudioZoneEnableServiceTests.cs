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
/// Unit tests for <see cref="AudioZoneEnableService"/>.
/// </summary>
public sealed class AudioZoneEnableServiceTests
{
    [Fact]
    public void Set_enqueues_Control_Set_with_boolean_on_the_registered_controlTag()
    {
        using CommandQueue queue = NewQueue();
        var zones = new AudioZoneRegistry("dsp-1");
        zones.TryRegister("mic1", "zoneA", "zone.mic1.A.enable");
        var sut = new AudioZoneEnableService("dsp-1", zones, queue, new IdGenerator());

        sut.Set("mic1", "zoneA", true);

        IReadOnlyList<JsonRpcRequest> sent = queue.SnapshotPending();
        sent.Should().HaveCount(1);
        sent[0].Method.Should().Be("Control.Set");
        var p = JObject.FromObject(sent[0].Params!);
        p["Name"]!.ToString().Should().Be("zone.mic1.A.enable");
        p["Value"]!.ToObject<bool>().Should().BeTrue();
    }

    [Fact]
    public void Set_updates_cache_optimistically()
    {
        using CommandQueue queue = NewQueue();
        var zones = new AudioZoneRegistry("dsp-1");
        zones.TryRegister("mic1", "zoneA", "zone.mic1.A.enable");
        var sut = new AudioZoneEnableService("dsp-1", zones, queue, new IdGenerator());

        sut.Set("mic1", "zoneA", true);
        sut.Query("mic1", "zoneA").Should().BeTrue();
    }

    [Fact]
    public void Toggle_from_false_sends_Control_Set_with_true_and_updates_cache()
    {
        using CommandQueue queue = NewQueue();
        var zones = new AudioZoneRegistry("dsp-1");
        zones.TryRegister("mic1", "zoneA", "zone.mic1.A.enable");
        var sut = new AudioZoneEnableService("dsp-1", zones, queue, new IdGenerator());

        sut.Toggle("mic1", "zoneA");

        IReadOnlyList<JsonRpcRequest> sent = queue.SnapshotPending();
        sent[0].Method.Should().Be("Control.Set");
        JObject.FromObject(sent[0].Params!)["Value"]!.ToObject<bool>().Should().BeTrue();
        sut.Query("mic1", "zoneA").Should().BeTrue();
    }

    [Fact]
    public void Toggle_from_true_sends_false()
    {
        using CommandQueue queue = NewQueue();
        var zones = new AudioZoneRegistry("dsp-1");
        zones.TryRegister("mic1", "zoneA", "zone.mic1.A.enable");
        var sut = new AudioZoneEnableService("dsp-1", zones, queue, new IdGenerator());

        sut.Set("mic1", "zoneA", true);
        sut.Toggle("mic1", "zoneA");

        IReadOnlyList<JsonRpcRequest> sent = queue.SnapshotPending();
        sent.Should().HaveCount(2);
        JObject.FromObject(sent[1].Params!)["Value"]!.ToObject<bool>().Should().BeFalse();
        sut.Query("mic1", "zoneA").Should().BeFalse();
    }

    [Fact]
    public void Query_unknown_pair_returns_false()
    {
        using CommandQueue queue = NewQueue();
        var sut = new AudioZoneEnableService("dsp-1", new AudioZoneRegistry("dsp-1"), queue, new IdGenerator());
        sut.Query("nope", "nope").Should().BeFalse();
    }

    [Fact]
    public void Set_unknown_pair_logs_error_and_does_not_enqueue()
    {
        using CommandQueue queue = NewQueue();
        var sut = new AudioZoneEnableService("dsp-1", new AudioZoneRegistry("dsp-1"), queue, new IdGenerator());
        sut.Set("nope", "nope", true);
        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void Toggle_unknown_pair_logs_error_and_does_not_enqueue()
    {
        using CommandQueue queue = NewQueue();
        var sut = new AudioZoneEnableService("dsp-1", new AudioZoneRegistry("dsp-1"), queue, new IdGenerator());
        sut.Toggle("nope", "nope");
        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void OnDeviceUpdate_fires_event_with_channelId_and_zoneId_args()
    {
        using CommandQueue queue = NewQueue();
        var zones = new AudioZoneRegistry("dsp-1");
        zones.TryRegister("mic1", "zoneA", "zone.mic1.A.enable");
        var sut = new AudioZoneEnableService("dsp-1", zones, queue, new IdGenerator());

        var raised = new List<(string, string)>();
        sut.ZoneEnableChanged += (_, args) => raised.Add((args.Arg1, args.Arg2));

        sut.OnDeviceUpdate(new ChangeGroupDelta("zone.mic1.A.enable", JToken.FromObject(true), null, null));

        raised.Should().Equal(("mic1", "zoneA"));
        sut.Query("mic1", "zoneA").Should().BeTrue();
    }

    [Fact]
    public void OnDeviceUpdate_no_change_does_not_re_raise_event()
    {
        using CommandQueue queue = NewQueue();
        var zones = new AudioZoneRegistry("dsp-1");
        zones.TryRegister("mic1", "zoneA", "zone.mic1.A.enable");
        var sut = new AudioZoneEnableService("dsp-1", zones, queue, new IdGenerator());

        int count = 0;
        sut.ZoneEnableChanged += (_, _) => count++;

        sut.OnDeviceUpdate(new ChangeGroupDelta("zone.mic1.A.enable", JToken.FromObject(true), null, null));
        sut.OnDeviceUpdate(new ChangeGroupDelta("zone.mic1.A.enable", JToken.FromObject(true), null, null));

        count.Should().Be(1);
    }

    [Fact]
    public void OnDeviceUpdate_unknown_tag_is_silent()
    {
        using CommandQueue queue = NewQueue();
        var sut = new AudioZoneEnableService("dsp-1", new AudioZoneRegistry("dsp-1"), queue, new IdGenerator());

        Action act = () => sut.OnDeviceUpdate(new ChangeGroupDelta("never-registered", JToken.FromObject(true), null, null));
        act.Should().NotThrow();
    }

    [Fact]
    public void OnDeviceUpdate_treats_int_one_as_true_and_int_zero_as_false_for_zone_enable()
    {
        using CommandQueue queue = NewQueue();
        var zones = new AudioZoneRegistry("dsp-1");
        zones.TryRegister("mic1", "zoneA", "zone.mic1.A.enable");
        var sut = new AudioZoneEnableService("dsp-1", zones, queue, new IdGenerator());

        sut.OnDeviceUpdate(new ChangeGroupDelta("zone.mic1.A.enable", JToken.FromObject(1), null, null));
        sut.Query("mic1", "zoneA").Should().BeTrue();

        sut.OnDeviceUpdate(new ChangeGroupDelta("zone.mic1.A.enable", JToken.FromObject(0), null, null));
        sut.Query("mic1", "zoneA").Should().BeFalse();
    }

    [Fact]
    public void Constructor_with_null_args_throws()
    {
        using CommandQueue q = NewQueue();
        Action a = () => _ = new AudioZoneEnableService(null!, new AudioZoneRegistry("d"), q, new IdGenerator());
        Action b = () => _ = new AudioZoneEnableService("d", null!, q, new IdGenerator());
        Action c = () => _ = new AudioZoneEnableService("d", new AudioZoneRegistry("d"), null!, new IdGenerator());
        Action e = () => _ = new AudioZoneEnableService("d", new AudioZoneRegistry("d"), q, null!);
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
