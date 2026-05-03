// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.Connectivity.Redundancy;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity.Redundancy;

/// <summary>
/// Unit tests for <see cref="SwitchbackPolicy"/>. The README behaviour
/// (default) and the QSC-recommended sticky-on-current behaviour both
/// have to be pinned because they materially affect failover semantics.
/// </summary>
public sealed class SwitchbackPolicyTests
{
    [Fact]
    public void Default_picks_Primary_when_only_Primary_is_Active()
    {
        SwitchbackPolicy.Default.PickActive(currentActive: null, EngineState.Active, EngineState.Standby)
            .Should().Be(CoreSlot.Primary);
    }

    [Fact]
    public void Default_picks_Backup_when_only_Backup_is_Active()
    {
        SwitchbackPolicy.Default.PickActive(currentActive: CoreSlot.Primary, EngineState.Standby, EngineState.Active)
            .Should().Be(CoreSlot.Backup);
    }

    [Fact]
    public void Default_returns_null_when_neither_slot_is_Active()
    {
        SwitchbackPolicy.Default.PickActive(currentActive: CoreSlot.Primary, EngineState.Standby, EngineState.Standby)
            .Should().BeNull();
    }

    [Fact]
    public void Default_switches_back_to_Primary_when_both_are_Active()
    {
        // README behaviour: primary returns to Active while backup is
        // also still reporting Active; switch back to primary.
        SwitchbackPolicy.Default.PickActive(currentActive: CoreSlot.Backup, EngineState.Active, EngineState.Active)
            .Should().Be(CoreSlot.Primary);
    }

    [Fact]
    public void QscRecommended_stays_on_current_when_both_are_Active()
    {
        SwitchbackPolicy.QscRecommended.PickActive(currentActive: CoreSlot.Backup, EngineState.Active, EngineState.Active)
            .Should().Be(CoreSlot.Backup);
    }

    [Fact]
    public void QscRecommended_picks_Backup_when_only_Backup_is_Active()
    {
        // Failover: even under sticky-on-current, when the current
        // active stops being Active, switch.
        SwitchbackPolicy.QscRecommended.PickActive(currentActive: CoreSlot.Primary, EngineState.Standby, EngineState.Active)
            .Should().Be(CoreSlot.Backup);
    }

    [Fact]
    public void QscRecommended_picks_Primary_at_startup_when_both_are_Active_and_no_current()
    {
        SwitchbackPolicy.QscRecommended.PickActive(currentActive: null, EngineState.Active, EngineState.Active)
            .Should().Be(CoreSlot.Primary);
    }

    [Fact]
    public void Unknown_states_count_as_not_Active()
    {
        SwitchbackPolicy.Default.PickActive(currentActive: null, EngineState.Unknown, EngineState.Active)
            .Should().Be(CoreSlot.Backup);

        SwitchbackPolicy.Default.PickActive(currentActive: null, EngineState.Idle, EngineState.Idle)
            .Should().BeNull();
    }
}
