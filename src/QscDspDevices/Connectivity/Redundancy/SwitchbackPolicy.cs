// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

namespace QscDspDevices.Connectivity.Redundancy;

/// <summary>
/// Decides which slot the redundant pair's "active" pointer should
/// move to given the current active and the latest observed
/// <see cref="EngineState"/> per slot.
/// </summary>
/// <remarks>
/// <para>
/// <b>README behaviour (default).</b> The README §"Device Connection"
/// requires the plugin to "switch back to the primary once it comes
/// back online" — so when the primary returns to <c>Active</c> after
/// a failover, the active flips back to primary even if the backup
/// is also still <c>Active</c>.
/// </para>
/// <para>
/// <b>QSC-guidance behaviour.</b> QSC's official guidance is that
/// "after a failover, Q-SYS does not automatically change back to
/// the failed Core when the Core recovers." Setting
/// <see cref="RespectQscFailoverGuidance"/> to <c>true</c> opts into
/// this sticky-on-current behaviour: the active stays put until the
/// current active itself stops being <c>Active</c>.
/// </para>
/// <para>
/// SPEC_COMPLIANCE.md deviation D3 documents the choice — we ship
/// the README behaviour by default and expose the flag as the
/// escape hatch.
/// </para>
/// </remarks>
public sealed record SwitchbackPolicy(bool RespectQscFailoverGuidance = false)
{
    /// <summary>Gets the default policy honouring the README's switch-back-to-primary rule.</summary>
    public static SwitchbackPolicy Default { get; } = new(RespectQscFailoverGuidance: false);

    /// <summary>Gets the QSC-recommended sticky-on-current behaviour policy.</summary>
    public static SwitchbackPolicy QscRecommended { get; } = new(RespectQscFailoverGuidance: true);

    /// <summary>
    /// Returns the slot the pair should make active given the current
    /// active and the latest observed states. Returns <c>null</c> when
    /// no slot should be active (no Core reporting <see cref="EngineState.Active"/>).
    /// </summary>
    /// <param name="currentActive">The currently-active slot, or null if none.</param>
    /// <param name="primaryState">The primary's most recently observed state.</param>
    /// <param name="backupState">The backup's most recently observed state.</param>
    /// <returns>The new active slot, or null.</returns>
    public CoreSlot? PickActive(CoreSlot? currentActive, EngineState primaryState, EngineState backupState)
    {
        bool primaryActive = primaryState == EngineState.Active;
        bool backupActive = backupState == EngineState.Active;

        if (!primaryActive && !backupActive)
        {
            // No Core is currently Active — refuse all writes until one returns.
            return null;
        }

        if (primaryActive && !backupActive)
        {
            return CoreSlot.Primary;
        }

        if (!primaryActive && backupActive)
        {
            return CoreSlot.Backup;
        }

        // Both Active — a transient during failover, or a Designer-side
        // misconfiguration. Tie-break:
        //  * README behaviour: prefer Primary (covers the switchback
        //    case when primary returns to Active while backup is also
        //    still reporting Active).
        //  * QSC-guidance behaviour: stick with the current active to
        //    avoid flapping; if neither is currently active, pick
        //    Primary (deterministic startup).
        if (RespectQscFailoverGuidance)
        {
            return currentActive ?? CoreSlot.Primary;
        }

        return CoreSlot.Primary;
    }
}
