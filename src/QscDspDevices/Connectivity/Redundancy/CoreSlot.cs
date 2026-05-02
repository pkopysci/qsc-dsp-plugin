// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

namespace QscDspDevices.Connectivity.Redundancy;

/// <summary>
/// Identifies one half of a redundant Core pair. The framework only
/// supports a single backup, so this is binary.
/// </summary>
public enum CoreSlot
{
    /// <summary>The primary Core (configured via <c>Initialize</c>).</summary>
    Primary,

    /// <summary>The backup Core (configured via <c>SetBackupDeviceConnection</c>).</summary>
    Backup,
}
