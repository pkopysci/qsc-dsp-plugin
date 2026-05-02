// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using QscDspDevices.LogicTriggers;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.Protocol.JsonRpc;
using Xunit;

namespace QscDspDevices.UnitTests.LogicTriggers;

/// <summary>
/// Unit tests for <see cref="LogicTriggerService"/>.
/// </summary>
public sealed class LogicTriggerServiceTests
{
    [Fact]
    public void Pulse_known_id_enqueues_Control_Set_with_Value_true()
    {
        using CommandQueue queue = NewQueue();
        var registry = new LogicTriggerRegistry("dsp-1");
        registry.Register("rec", "rec.start");
        var sut = new LogicTriggerService("dsp-1", registry, queue, new IdGenerator());

        sut.Pulse("rec");

        IReadOnlyList<JsonRpcRequest> sent = queue.SnapshotPending();
        sent.Should().HaveCount(1);
        sent[0].Method.Should().Be("Control.Set");
        var p = JObject.FromObject(sent[0].Params!);
        p["Name"]!.ToString().Should().Be("rec.start");
        p["Value"]!.ToObject<bool>().Should().BeTrue();
    }

    [Fact]
    public void Pulse_unknown_id_logs_error_and_does_not_enqueue()
    {
        using CommandQueue queue = NewQueue();
        var sut = new LogicTriggerService("dsp-1", new LogicTriggerRegistry("dsp-1"), queue, new IdGenerator());

        sut.Pulse("nope");

        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void OnDeviceUpdate_known_tag_fires_event_with_trigger_id()
    {
        using CommandQueue queue = NewQueue();
        var registry = new LogicTriggerRegistry("dsp-1");
        registry.Register("rec", "rec.start");
        var sut = new LogicTriggerService("dsp-1", registry, queue, new IdGenerator());

        var raised = new List<string>();
        sut.LogicTriggerStateChanged += (_, args) => raised.Add(args.Arg);

        sut.OnDeviceUpdate(new ChangeGroupDelta("rec.start", JToken.FromObject(true), null, null));

        raised.Should().Equal("rec");
    }

    [Fact]
    public void OnDeviceUpdate_fires_event_on_every_delta_no_coalescing()
    {
        // Per spec: trigger event fires on every AutoPoll delta, not just
        // transitions. Two deltas with the same value still fire twice
        // because the framework's Single-arg event signals "transitioned",
        // not "the new value is X" — and consecutive pulses on a momentary
        // trigger that auto-resets are legitimate distinct fires.
        using CommandQueue queue = NewQueue();
        var registry = new LogicTriggerRegistry("dsp-1");
        registry.Register("rec", "rec.start");
        var sut = new LogicTriggerService("dsp-1", registry, queue, new IdGenerator());

        int count = 0;
        sut.LogicTriggerStateChanged += (_, _) => count++;

        sut.OnDeviceUpdate(new ChangeGroupDelta("rec.start", JToken.FromObject(true), null, null));
        sut.OnDeviceUpdate(new ChangeGroupDelta("rec.start", JToken.FromObject(true), null, null));

        count.Should().Be(2);
    }

    [Fact]
    public void OnDeviceUpdate_unknown_tag_is_silent()
    {
        using CommandQueue queue = NewQueue();
        var sut = new LogicTriggerService("dsp-1", new LogicTriggerRegistry("dsp-1"), queue, new IdGenerator());

        Action act = () => sut.OnDeviceUpdate(new ChangeGroupDelta("never-registered", JToken.FromObject(true), null, null));
        act.Should().NotThrow();
    }

    [Fact]
    public void Pulse_with_null_id_throws()
    {
        using CommandQueue queue = NewQueue();
        var sut = new LogicTriggerService("dsp-1", new LogicTriggerRegistry("dsp-1"), queue, new IdGenerator());
        Action act = () => sut.Pulse(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_with_null_args_throws()
    {
        using CommandQueue q = NewQueue();
        Action a = () => _ = new LogicTriggerService(null!, new LogicTriggerRegistry("d"), q, new IdGenerator());
        Action b = () => _ = new LogicTriggerService("d", null!, q, new IdGenerator());
        Action c = () => _ = new LogicTriggerService("d", new LogicTriggerRegistry("d"), null!, new IdGenerator());
        Action e = () => _ = new LogicTriggerService("d", new LogicTriggerRegistry("d"), q, null!);
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
