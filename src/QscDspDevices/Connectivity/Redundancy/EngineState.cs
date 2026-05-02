// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

namespace QscDspDevices.Connectivity.Redundancy;

/// <summary>
/// The three states a Q-SYS Core's <c>EngineStatus.State</c> field can
/// report (per <c>research/QRC_PROTOCOL.md</c> §8). <see cref="Unknown"/>
/// is the plugin-side initial value before any push has been observed.
/// </summary>
public enum EngineState
{
    /// <summary>Plugin-side initial value; no <c>EngineStatus</c> push has been parsed yet.</summary>
    Unknown,

    /// <summary>The Core is booting / has no design loaded.</summary>
    Idle,

    /// <summary>The Core is the standby of a redundant pair (or, for non-redundant, in a failed/refusing state).</summary>
    Standby,

    /// <summary>The Core is the currently-active half of the pair (or, for non-redundant, fully operational).</summary>
    Active,
}
