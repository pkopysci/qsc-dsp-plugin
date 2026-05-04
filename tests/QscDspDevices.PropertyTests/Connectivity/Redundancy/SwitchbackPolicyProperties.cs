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
    /// <param name="currentIsNull">Picks the null shape of <c>currentActive</c>.</param>
    /// <param name="currentIsBackup">Picks Backup vs Primary when current is non-null.</param>
    /// <param name="primaryRaw">FsCheck-supplied int folded into the primary slot's <see cref="EngineState"/>.</param>
    /// <param name="backupRaw">FsCheck-supplied int folded into the backup slot's <see cref="EngineState"/>.</param>
    /// <param name="qscRecommended">Picks the policy variant under test.</param>
    /// <returns>Always true — the assertion is "did not throw".</returns>
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
    /// <param name="currentIsNull">Drives whether <c>currentActive</c> is null or non-null.</param>
    /// <param name="currentIsBackup">Drives Backup vs Primary selection for the non-null case.</param>
    /// <param name="primaryRaw">Folded by <c>ToEngineState</c> for the primary slot.</param>
    /// <param name="backupRaw">Folded by <c>ToEngineState</c> for the backup slot.</param>
    /// <param name="qscRecommended">Selects between the README and QSC-recommended switchback variants.</param>
    /// <returns>True iff both calls return the same slot.</returns>
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
    /// <param name="currentIsNull">Drives the null vs non-null shape of the <c>currentActive</c> argument.</param>
    /// <param name="currentIsBackup">Disambiguates Backup vs Primary when <c>currentActive</c> is non-null.</param>
    /// <param name="primaryRaw">Random int mapped to the primary slot's <see cref="EngineState"/>.</param>
    /// <param name="backupRaw">Random int mapped to the backup slot's <see cref="EngineState"/>.</param>
    /// <param name="qscRecommended">Selects the switchback variant to exercise.</param>
    /// <returns>True iff the returned slot has Active state, or null was returned.</returns>
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
