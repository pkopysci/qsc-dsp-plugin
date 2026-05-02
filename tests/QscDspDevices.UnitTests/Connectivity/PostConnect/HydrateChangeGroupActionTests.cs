// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using QscDspDevices.AudioControl;
using QscDspDevices.Connectivity.PostConnect;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.Protocol.JsonRpc;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity.PostConnect;

/// <summary>
/// Unit tests for <see cref="HydrateChangeGroupAction"/>.
/// </summary>
public sealed class HydrateChangeGroupActionTests
{
    private static readonly IReadOnlyList<string> NoTags = Array.Empty<string>();

    [Fact]
    public async Task Hydration_with_no_channels_skips_silently()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        var manager = new ChangeGroupManager("dsp-1", new IdGenerator());
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var sut = new HydrateChangeGroupAction("dsp-1", registry, manager, queue, dispatcher, logon: null);

        await sut.RunAsync(CancellationToken.None);

        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public async Task Hydration_enqueues_AddControl_per_tag_then_AutoPoll()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 0, NoTags));
        registry.RegisterOutput(new AudioChannel("out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags));

        var manager = new ChangeGroupManager("dsp-1", new IdGenerator());
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var sut = new HydrateChangeGroupAction("dsp-1", registry, manager, queue, dispatcher, logon: null);

        await sut.RunAsync(CancellationToken.None);

        IReadOnlyList<JsonRpcRequest> sent = queue.SnapshotPending();
        sent.Should().HaveCount(5); // 4 AddControl + 1 AutoPoll
        for (int i = 0; i < 4; i++)
        {
            sent[i].Method.Should().Be("ChangeGroup.AddControl");
        }

        sent[^1].Method.Should().Be("ChangeGroup.AutoPoll");

        // Verify the AutoPoll names the right group and rate.
        var p = JObject.FromObject(sent[^1].Params!);
        p["Id"]!.ToString().Should().Be(ChangeGroupManager.PluginGroupId);
        p["Rate"]!.ToObject<double>().Should().BeApproximately(0.25, 1e-9);
    }

    [Fact]
    public async Task Hydration_waits_for_LogonAction_completion_when_supplied()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("dsp-1");
        registry.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 0, NoTags));

        var manager = new ChangeGroupManager("dsp-1", new IdGenerator());
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var ids = new IdGenerator();
        var logon = new LogonAction("dsp-1", () => new LogonCredentials("u", "p"), queue, dispatcher, ids);

        // Kick off Logon — it will sit waiting on a response that never arrives
        // until we feed one. Then start hydration concurrently and verify it
        // does not enqueue any subscribe before Logon settles.
        Task logonTask = logon.RunAsync(CancellationToken.None);
        await Task.Delay(50);
        var sentBeforeHydrate = queue.SnapshotPending().ToList();
        sentBeforeHydrate.Should().HaveCount(1).And.Contain(r => r.Method == "Logon");

        var sut = new HydrateChangeGroupAction("dsp-1", registry, manager, queue, dispatcher, logon);
        Task hydrate = sut.RunAsync(CancellationToken.None);

        // 100 ms in, hydration must NOT have enqueued anything past the Logon.
        await Task.Delay(100);
        queue.SnapshotPending().Should().BeEmpty(
            "hydration must not enqueue subscribe requests before Logon completes");

        // Feed the Logon response — both should now finish, and hydration
        // should now enqueue 2 AddControl + 1 AutoPoll.
        long logonId = sentBeforeHydrate[0].Id;
        dispatcher.Dispatch($"{{\"jsonrpc\":\"2.0\",\"id\":{logonId},\"result\":true}}");

        await logonTask;
        await hydrate;

        IReadOnlyList<JsonRpcRequest> afterAll = queue.SnapshotPending();
        afterAll.Should().HaveCount(3);
        afterAll[2].Method.Should().Be("ChangeGroup.AutoPoll");
    }

    [Fact]
    public void Constructor_with_null_required_args_throws()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("d");
        var manager = new ChangeGroupManager("d", new IdGenerator());
        var dispatcher = new JsonRpcDispatcher("d");

        Action a = () => _ = new HydrateChangeGroupAction(null!, registry, manager, queue, dispatcher, null);
        Action b = () => _ = new HydrateChangeGroupAction("d", null!, manager, queue, dispatcher, null);
        Action c = () => _ = new HydrateChangeGroupAction("d", registry, null!, queue, dispatcher, null);
        Action e = () => _ = new HydrateChangeGroupAction("d", registry, manager, null!, dispatcher, null);
        Action f = () => _ = new HydrateChangeGroupAction("d", registry, manager, queue, null!, null);

        a.Should().Throw<ArgumentNullException>();
        b.Should().Throw<ArgumentNullException>();
        c.Should().Throw<ArgumentNullException>();
        e.Should().Throw<ArgumentNullException>();
        f.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Cancelled_token_propagates()
    {
        using CommandQueue queue = NewQueue();
        var registry = new AudioChannelRegistry("d");
        var manager = new ChangeGroupManager("d", new IdGenerator());
        var dispatcher = new JsonRpcDispatcher("d");
        var sut = new HydrateChangeGroupAction("d", registry, manager, queue, dispatcher, null);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Func<Task> act = async () => await sut.RunAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static CommandQueue NewQueue()
    {
        var q = new CommandQueue("dsp-1");
        q.StartAccepting();
        return q;
    }
}
