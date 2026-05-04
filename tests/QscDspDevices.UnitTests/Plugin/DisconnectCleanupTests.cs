// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.Protocol.JsonRpc;
using QscDspDevices.TestSupport.Transport;
using Xunit;

namespace QscDspDevices.UnitTests.Plugin;

public sealed class DisconnectCleanupTests
{
    [Fact]
    public void TryEnqueueDestroy_with_unknown_group_is_a_noop()
    {
        const string deviceId = "dsp-1";
        using var queue = new CommandQueue(deviceId);
        queue.StartAccepting();
        var manager = new ChangeGroupManager(deviceId, new IdGenerator());

        // Manager has never seen the plugin group: BuildDestroy returns null
        // and the helper returns silently.
        DisconnectCleanup.TryEnqueueDestroy(deviceId, manager, queue, transport: null);

        IReadOnlyList<JsonRpcRequest> snapshot = queue.SnapshotPending();
        snapshot.Should().BeEmpty();
    }

    [Fact]
    public void TryEnqueueDestroy_enqueues_when_group_exists_and_queue_is_accepting()
    {
        const string deviceId = "dsp-1";
        using var queue = new CommandQueue(deviceId);
        queue.StartAccepting();
        var manager = new ChangeGroupManager(deviceId, new IdGenerator());

        // Seed the manager so BuildDestroy returns a request.
        JsonRpcRequest? add = manager.BuildAddControl(ChangeGroupManager.PluginGroupId, "Input.1.gain");
        add.Should().NotBeNull();

        DisconnectCleanup.TryEnqueueDestroy(deviceId, manager, queue, transport: null);

        IReadOnlyList<JsonRpcRequest> snapshot = queue.SnapshotPending();
        snapshot.Should().Contain(r => r.Method == "ChangeGroup.Destroy");
    }

    [Fact]
    public void TryEnqueueDestroy_skips_when_transport_supplied_and_disconnected()
    {
        const string deviceId = "dsp-1";
        using var queue = new CommandQueue(deviceId);
        queue.StartAccepting();
        using var transport = new StubTransport();
        var manager = new ChangeGroupManager(deviceId, new IdGenerator());
        manager.BuildAddControl(ChangeGroupManager.PluginGroupId, "Input.1.gain");

        // Transport never simulated connect → IsConnected is false.
        DisconnectCleanup.TryEnqueueDestroy(deviceId, manager, queue, transport);

        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void TryEnqueueDestroy_with_null_inputs_does_not_throw()
    {
        Action act1 = () => DisconnectCleanup.TryEnqueueDestroy("dsp-1", null, null, null);
        act1.Should().NotThrow();
    }
}
