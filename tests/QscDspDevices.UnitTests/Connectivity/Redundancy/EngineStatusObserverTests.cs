// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using FluentAssertions;
using QscDspDevices.Connectivity.Redundancy;
using QscDspDevices.Protocol;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity.Redundancy;

/// <summary>
/// Unit tests for <see cref="EngineStatusObserver"/>.
/// </summary>
public sealed class EngineStatusObserverTests
{
    [Fact]
    public void Active_State_push_invokes_callback_with_Active()
    {
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var observed = new List<EngineState>();
        using var sut = new EngineStatusObserver("dsp-1", dispatcher, observed.Add);

        dispatcher.Dispatch("""{"jsonrpc":"2.0","method":"EngineStatus","params":{"State":"Active"}}""");

        observed.Should().Equal(EngineState.Active);
    }

    [Fact]
    public void Standby_State_push_invokes_callback_with_Standby()
    {
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var observed = new List<EngineState>();
        using var sut = new EngineStatusObserver("dsp-1", dispatcher, observed.Add);

        dispatcher.Dispatch("""{"jsonrpc":"2.0","method":"EngineStatus","params":{"State":"Standby"}}""");

        observed.Should().Equal(EngineState.Standby);
    }

    [Fact]
    public void Idle_State_push_invokes_callback_with_Idle()
    {
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var observed = new List<EngineState>();
        using var sut = new EngineStatusObserver("dsp-1", dispatcher, observed.Add);

        dispatcher.Dispatch("""{"jsonrpc":"2.0","method":"EngineStatus","params":{"State":"Idle"}}""");

        observed.Should().Equal(EngineState.Idle);
    }

    [Fact]
    public void Notification_with_method_other_than_EngineStatus_is_ignored()
    {
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        bool fired = false;
        using var sut = new EngineStatusObserver("dsp-1", dispatcher, _ => fired = true);

        dispatcher.Dispatch("""{"jsonrpc":"2.0","method":"SomethingElse","params":{"State":"Active"}}""");

        fired.Should().BeFalse();
    }

    [Fact]
    public void EngineStatus_push_with_no_State_field_is_silently_logged()
    {
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        bool fired = false;
        using var sut = new EngineStatusObserver("dsp-1", dispatcher, _ => fired = true);

        dispatcher.Dispatch("""{"jsonrpc":"2.0","method":"EngineStatus","params":{"DesignName":"X"}}""");

        fired.Should().BeFalse();
    }

    [Fact]
    public void EngineStatus_push_with_unknown_State_value_is_silently_logged()
    {
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        bool fired = false;
        using var sut = new EngineStatusObserver("dsp-1", dispatcher, _ => fired = true);

        dispatcher.Dispatch("""{"jsonrpc":"2.0","method":"EngineStatus","params":{"State":"Frobnicating"}}""");

        fired.Should().BeFalse();
    }

    [Fact]
    public void Dispose_unsubscribes_from_the_dispatcher()
    {
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        bool fired = false;
        var sut = new EngineStatusObserver("dsp-1", dispatcher, _ => fired = true);
        sut.Dispose();

        dispatcher.Dispatch("""{"jsonrpc":"2.0","method":"EngineStatus","params":{"State":"Active"}}""");

        fired.Should().BeFalse();
    }

    [Fact]
    public void Dispose_is_idempotent()
    {
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var sut = new EngineStatusObserver("dsp-1", dispatcher, _ => { });

        sut.Dispose();
        Action act = () => sut.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_with_null_args_throws()
    {
        var dispatcher = new JsonRpcDispatcher("d");
        Action a = () => _ = new EngineStatusObserver(null!, dispatcher, _ => { });
        Action b = () => _ = new EngineStatusObserver("d", null!, _ => { });
        Action c = () => _ = new EngineStatusObserver("d", dispatcher, null!);
        a.Should().Throw<ArgumentNullException>();
        b.Should().Throw<ArgumentNullException>();
        c.Should().Throw<ArgumentNullException>();
    }
}
