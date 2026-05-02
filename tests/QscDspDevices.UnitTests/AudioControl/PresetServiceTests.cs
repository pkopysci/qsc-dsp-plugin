// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using QscDspDevices.AudioControl;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.JsonRpc;
using Xunit;

namespace QscDspDevices.UnitTests.AudioControl;

/// <summary>
/// Unit tests for <see cref="PresetService"/>.
/// </summary>
public sealed class PresetServiceTests
{
    [Fact]
    public void Recall_known_preset_enqueues_Snapshot_Load()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        var sut = new PresetService("dsp-1", registry, queue, new IdGenerator());
        registry.RegisterPreset(new AudioPreset("dinner", "MainBank", 3));

        sut.Recall("dinner");

        IReadOnlyList<JsonRpcRequest> sent = queue.SnapshotPending();
        sent.Should().HaveCount(1);
        sent[0].Method.Should().Be("Snapshot.Load");

        var p = JObject.FromObject(sent[0].Params!);
        p["Name"]!.ToString().Should().Be("MainBank");
        p["Bank"]!.ToObject<int>().Should().Be(3);
        p.ContainsKey("Ramp").Should().BeFalse();
    }

    [Fact]
    public void Recall_unknown_preset_does_not_enqueue_anything()
    {
        using CommandQueue queue = NewQueue();
        var sut = new PresetService("dsp-1", new AudioChannelRegistry("dsp-1"), queue, new IdGenerator());

        sut.Recall("nope");

        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void Recall_with_null_id_throws()
    {
        using CommandQueue queue = NewQueue();
        var sut = new PresetService("dsp-1", new AudioChannelRegistry("dsp-1"), queue, new IdGenerator());
        Action act = () => sut.Recall(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_with_null_args_throws()
    {
        using CommandQueue q = NewQueue();
        Action a = () => _ = new PresetService(null!, new AudioChannelRegistry("d"), q, new IdGenerator());
        Action b = () => _ = new PresetService("d", null!, q, new IdGenerator());
        Action c = () => _ = new PresetService("d", new AudioChannelRegistry("d"), null!, new IdGenerator());
        Action e = () => _ = new PresetService("d", new AudioChannelRegistry("d"), q, null!);
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
