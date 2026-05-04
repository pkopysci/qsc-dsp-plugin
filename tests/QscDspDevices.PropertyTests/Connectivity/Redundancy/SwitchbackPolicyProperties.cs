// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FsCheck.Xunit;
using QscDspDevices.Connectivity.Redundancy;
using QscDspDevices.Protocol;

namespace QscDspDevices.PropertyTests.Connectivity.Redundancy;

/// <summary>
/// FsCheck property tests for <see cref="SwitchbackPolicy.PickActive"/>.
/// The decision function is a pure transform over the
/// (currentActive, primaryState, backupState) cross-product; example-
/// based tests pin the named scenarios, properties pin the
/// invariants over every valid input.
/// </summary>
public class SwitchbackPolicyProperties
{
    /// <summary>
    /// PickActive is total: it never throws on any valid input, for
    /// either policy variant.
    /// </summary>
    /// <param name="currentIsNull">If true, currentActive is null; else picked from primary/backup.</param>
    /// <param name="currentIsBackup">When currentIsNull is false, picks Backup; else Primary.</param>
    /// <param name="primaryRaw">Raw int folded into a valid EngineState.</param>
    /// <param name="backupRaw">Raw int folded into a valid EngineState.</param>
    /// <param name="qscRecommended">Switchback variant.</param>
    /// <returns>True (any non-throwing return is a pass).</returns>
    [Property]
    public bool PickActive_is_total(bool currentIsNull, bool currentIsBackup, int primaryRaw, int backupRaw, bool qscRecommended)
    {
        CoreSlot? current = currentIsNull
            ? null
            : currentIsBackup ? CoreSlot.Backup : CoreSlot.Primary;
        EngineState primary = ToEngineState(primaryRaw);
        EngineState backup = ToEngineState(backupRaw);
        SwitchbackPolicy policy = qscRecommended ? SwitchbackPolicy.QscRecommended : SwitchbackPolicy.Default;

        _ = policy.PickActive(current, primary, backup);
        return true;
    }

    /// <summary>
    /// PickActive is idempotent: calling it twice with the same args
    /// returns the same result. Guards against accidental hidden state.
    /// </summary>
    /// <param name="currentIsNull">If true, currentActive is null; else picked from primary/backup.</param>
    /// <param name="currentIsBackup">When currentIsNull is false, picks Backup; else Primary.</param>
    /// <param name="primaryRaw">Raw int folded into a valid EngineState.</param>
    /// <param name="backupRaw">Raw int folded into a valid EngineState.</param>
    /// <param name="qscRecommended">Switchback variant.</param>
    /// <returns>True if both calls return the same slot.</returns>
    [Property]
    public bool PickActive_is_idempotent(bool currentIsNull, bool currentIsBackup, int primaryRaw, int backupRaw, bool qscRecommended)
    {
        CoreSlot? current = currentIsNull
            ? null
            : currentIsBackup ? CoreSlot.Backup : CoreSlot.Primary;
        EngineState primary = ToEngineState(primaryRaw);
        EngineState backup = ToEngineState(backupRaw);
        SwitchbackPolicy policy = qscRecommended ? SwitchbackPolicy.QscRecommended : SwitchbackPolicy.Default;

        CoreSlot? a = policy.PickActive(current, primary, backup);
        CoreSlot? b = policy.PickActive(current, primary, backup);
        return a == b;
    }

    /// <summary>
    /// Whenever PickActive returns a non-null slot, that slot's state
    /// must be Active. (Negative invariant: never promote a non-Active
    /// slot.)
    /// </summary>
    /// <param name="currentIsNull">If true, currentActive is null; else picked from primary/backup.</param>
    /// <param name="currentIsBackup">When currentIsNull is false, picks Backup; else Primary.</param>
    /// <param name="primaryRaw">Raw int folded into a valid EngineState.</param>
    /// <param name="backupRaw">Raw int folded into a valid EngineState.</param>
    /// <param name="qscRecommended">Switchback variant.</param>
    /// <returns>True if the chosen slot's state is Active, or null was returned.</returns>
    [Property]
    public bool PickActive_only_returns_an_Active_slot(bool currentIsNull, bool currentIsBackup, int primaryRaw, int backupRaw, bool qscRecommended)
    {
        CoreSlot? current = currentIsNull
            ? null
            : currentIsBackup ? CoreSlot.Backup : CoreSlot.Primary;
        EngineState primary = ToEngineState(primaryRaw);
        EngineState backup = ToEngineState(backupRaw);
        SwitchbackPolicy policy = qscRecommended ? SwitchbackPolicy.QscRecommended : SwitchbackPolicy.Default;

        CoreSlot? chosen = policy.PickActive(current, primary, backup);
        if (chosen is null)
        {
            return true;
        }

        EngineState chosenState = chosen == CoreSlot.Primary ? primary : backup;
        return chosenState == EngineState.Active;
    }

    private static EngineState ToEngineState(int raw)
    {
        // Map the FsCheck random int to one of the four legal states.
        int idx = ((raw % 4) + 4) % 4;
        return idx switch
        {
            0 => EngineState.Active,
            1 => EngineState.Standby,
            2 => EngineState.Idle,
            _ => EngineState.Unknown,
        };
    }
}
