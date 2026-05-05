// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.Connectivity.Ecp;
using QscDspDevices.Connectivity.Redundancy;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.Ecp;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity.Ecp;

public sealed class EcpEngineStateProbeTests
{
    [Fact]
    public void Inbound_sr_with_IsActive_1_raises_Active()
    {
        const string deviceId = "dsp-1";
        using var queue = new EcpCommandQueue(deviceId);
        var dispatcher = new EcpDispatcher(deviceId);
        EngineState? observed = null;
        using var sut = new EcpEngineStateProbe(deviceId, dispatcher, queue, s => observed = s);

        dispatcher.Dispatch("sr \"design\" \"id\" 1 1");

        observed.Should().Be(EngineState.Active);
    }

    [Fact]
    public void Inbound_sr_with_IsActive_0_raises_Standby()
    {
        const string deviceId = "dsp-1";
        using var queue = new EcpCommandQueue(deviceId);
        var dispatcher = new EcpDispatcher(deviceId);
        EngineState? observed = null;
        using var sut = new EcpEngineStateProbe(deviceId, dispatcher, queue, s => observed = s);

        dispatcher.Dispatch("sr \"design\" \"id\" 0 0");

        observed.Should().Be(EngineState.Standby);
    }

    [Fact]
    public void Repeated_sr_with_unchanged_state_does_not_re_raise()
    {
        const string deviceId = "dsp-1";
        using var queue = new EcpCommandQueue(deviceId);
        var dispatcher = new EcpDispatcher(deviceId);
        int count = 0;
        using var sut = new EcpEngineStateProbe(deviceId, dispatcher, queue, _ => count++);

        dispatcher.Dispatch("sr \"d\" \"id\" 1 1");
        dispatcher.Dispatch("sr \"d\" \"id\" 1 1");
        dispatcher.Dispatch("sr \"d\" \"id\" 1 1");

        count.Should().Be(1);
    }

    [Fact]
    public void Active_then_Standby_raises_both_transitions()
    {
        const string deviceId = "dsp-1";
        using var queue = new EcpCommandQueue(deviceId);
        var dispatcher = new EcpDispatcher(deviceId);
        var transitions = new List<EngineState>();
        using var sut = new EcpEngineStateProbe(deviceId, dispatcher, queue, s => transitions.Add(s));

        dispatcher.Dispatch("sr \"d\" \"id\" 1 1");
        dispatcher.Dispatch("sr \"d\" \"id\" 1 0");

        transitions.Should().Equal(EngineState.Active, EngineState.Standby);
    }

    [Fact]
    public void Non_sr_responses_are_ignored()
    {
        const string deviceId = "dsp-1";
        using var queue = new EcpCommandQueue(deviceId);
        var dispatcher = new EcpDispatcher(deviceId);
        int count = 0;
        using var sut = new EcpEngineStateProbe(deviceId, dispatcher, queue, _ => count++);

        dispatcher.Dispatch("cv \"x\" \"\" 0 0");
        dispatcher.Dispatch("cgpa");
        dispatcher.Dispatch("login_required");

        count.Should().Be(0);
    }

    [Fact]
    public void Constructor_validates_arguments()
    {
        const string deviceId = "dsp-1";
        using var queue = new EcpCommandQueue(deviceId);
        var dispatcher = new EcpDispatcher(deviceId);

        ((Action)(() => { _ = new EcpEngineStateProbe(null!, dispatcher, queue, _ => { }); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpEngineStateProbe(deviceId, null!, queue, _ => { }); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpEngineStateProbe(deviceId, dispatcher, null!, _ => { }); })).Should().Throw<ArgumentNullException>();
        ((Action)(() => { _ = new EcpEngineStateProbe(deviceId, dispatcher, queue, null!); })).Should().Throw<ArgumentNullException>();
    }
}
