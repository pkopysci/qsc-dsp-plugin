// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.Connectivity.Ecp;
using QscDspDevices.LogicTriggers;
using QscDspDevices.LogicTriggers.Ecp;
using Xunit;

namespace QscDspDevices.UnitTests.LogicTriggers.Ecp;

public sealed class EcpLogicTriggerServiceTests
{
    [Fact]
    public void Pulse_emits_ct_against_named_trigger_tag()
    {
        const string deviceId = "dsp-1";
        var registry = new LogicTriggerRegistry(deviceId);
        registry.Register("trigger1", "Logic.button.1");
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpLogicTriggerService(deviceId, registry, queue);

        sut.Pulse("trigger1");

        queue.SnapshotPending().Should().Equal("ct \"Logic.button.1\"");
    }

    [Fact]
    public void Pulse_with_unknown_id_logs_error_and_does_not_enqueue()
    {
        const string deviceId = "dsp-1";
        var registry = new LogicTriggerRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);
        queue.StartAccepting();
        var sut = new EcpLogicTriggerService(deviceId, registry, queue);

        sut.Pulse("nope");

        queue.SnapshotPending().Should().BeEmpty();
    }

    [Fact]
    public void Constructor_validates_arguments()
    {
        const string deviceId = "dsp-1";
        var registry = new LogicTriggerRegistry(deviceId);
        using var queue = new EcpCommandQueue(deviceId);

        ((Action)(() => { _ = new EcpLogicTriggerService(null!, registry, queue); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpLogicTriggerService(deviceId, null!, queue); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpLogicTriggerService(deviceId, registry, null!); })).Should().Throw<ArgumentNullException>();
    }
}
