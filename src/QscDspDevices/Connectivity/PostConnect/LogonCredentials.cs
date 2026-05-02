// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

namespace QscDspDevices.Connectivity.PostConnect;

/// <summary>
/// Username/password pair captured by <c>QscDspTcp.Initialize</c> and
/// supplied to <see cref="LogonAction"/>. Both fields are nullable —
/// when both are empty the post-connect action skips the
/// <c>Logon</c> step (the QSC Core treats Logon as optional unless
/// the design has Access Control configured; see
/// <c>research/QRC_PROTOCOL.md</c> §2).
/// </summary>
/// <param name="Username">The user name, or empty/null when not configured.</param>
/// <param name="Password">The password / PIN, or empty/null when not configured.</param>
public sealed record LogonCredentials(string? Username, string? Password)
{
    /// <summary>
    /// Gets a value indicating whether the credentials should trigger a
    /// <c>Logon</c> request. True iff at least one of the two fields is
    /// non-empty (we send Logon if either side is provided so a
    /// password-only PIN flow works).
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password);

    /// <summary>Gets the empty credentials sentinel — used before Initialize lands data.</summary>
    public static LogonCredentials Empty { get; } = new(null, null);
}
