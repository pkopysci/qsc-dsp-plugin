// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Globalization;

namespace QscDspDevices.Protocol.Ecp;

/// <summary>
/// Strongly-typed ECP command builder. Each static method returns the
/// wire text for one ECP §3 command, ready to hand to <see cref="EcpFramer.Encode(string)"/>.
/// </summary>
/// <remarks>
/// Strings are quoted unconditionally (the Core accepts quoted forms for
/// every parameter and they're the only safe option for control names
/// containing spaces). Quoting goes through <see cref="EcpQuoting.Escape(string)"/>.
/// Numbers are formatted with <see cref="CultureInfo.InvariantCulture"/>
/// so locale settings can never produce comma-decimal floats on the
/// wire.
/// </remarks>
internal static class EcpCommand
{
    /// <summary>Status Get. Builds <c>sg</c>.</summary>
    /// <returns>The wire text.</returns>
    public static string StatusGet() => "sg";

    /// <summary>Login. Builds <c>login NAME PIN</c>.</summary>
    /// <param name="name">User name. Whitespace-tolerant; quoted on the wire.</param>
    /// <param name="pin">PIN.</param>
    /// <returns>The wire text.</returns>
    /// <exception cref="ArgumentNullException">If either argument is null.</exception>
    public static string Login(string name, string pin)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(pin);
        return $"login {Quote(name)} {Quote(pin)}";
    }

    /// <summary>Control Get. Builds <c>cg "CONTROL_ID"</c>.</summary>
    /// <param name="controlId">The control name.</param>
    /// <returns>The wire text.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="controlId"/> is null.</exception>
    public static string ControlGet(string controlId)
    {
        ArgumentNullException.ThrowIfNull(controlId);
        return $"cg {Quote(controlId)}";
    }

    /// <summary>Control Set Value. Builds <c>csv "CONTROL_ID" VALUE</c>.</summary>
    /// <param name="controlId">The control name.</param>
    /// <param name="value">The numeric value.</param>
    /// <returns>The wire text.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="controlId"/> is null.</exception>
    public static string ControlSetValue(string controlId, double value)
    {
        ArgumentNullException.ThrowIfNull(controlId);
        return $"csv {Quote(controlId)} {value.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>Control Set String. Builds <c>css "CONTROL_ID" "STRING"</c>.</summary>
    /// <param name="controlId">The control name.</param>
    /// <param name="value">The string value (display-format, e.g. "5dB" or "true").</param>
    /// <returns>The wire text.</returns>
    /// <exception cref="ArgumentNullException">If either argument is null.</exception>
    public static string ControlSetString(string controlId, string value)
    {
        ArgumentNullException.ThrowIfNull(controlId);
        ArgumentNullException.ThrowIfNull(value);
        return $"css {Quote(controlId)} {Quote(value)}";
    }

    /// <summary>Control Set Position. Builds <c>csp "CONTROL_ID" POSITION</c>.</summary>
    /// <param name="controlId">The control name.</param>
    /// <param name="position">The position (0.0 — 1.0).</param>
    /// <returns>The wire text.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="controlId"/> is null.</exception>
    public static string ControlSetPosition(string controlId, double position)
    {
        ArgumentNullException.ThrowIfNull(controlId);
        return $"csp {Quote(controlId)} {position.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>Control Trigger. Builds <c>ct "CONTROL_ID"</c>.</summary>
    /// <param name="controlId">The control name to pulse.</param>
    /// <returns>The wire text.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="controlId"/> is null.</exception>
    public static string ControlTrigger(string controlId)
    {
        ArgumentNullException.ThrowIfNull(controlId);
        return $"ct {Quote(controlId)}";
    }

    /// <summary>Snapshot Load. Builds <c>ssl "BANK" NUMBER RAMP_SEC</c>.</summary>
    /// <param name="bank">The named bank.</param>
    /// <param name="number">The snapshot number within the bank.</param>
    /// <param name="rampSeconds">Optional ramp time; 0 for instant.</param>
    /// <returns>The wire text.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="bank"/> is null.</exception>
    public static string SnapshotLoad(string bank, int number, double rampSeconds = 0)
    {
        ArgumentNullException.ThrowIfNull(bank);
        return $"ssl {Quote(bank)} {number.ToString(CultureInfo.InvariantCulture)} {rampSeconds.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>Change Group Create. Builds <c>cgc GROUP_ID</c>.</summary>
    /// <param name="groupId">The 32-bit unsigned group id.</param>
    /// <returns>The wire text.</returns>
    public static string ChangeGroupCreate(uint groupId)
        => $"cgc {groupId.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>Change Group Add. Builds <c>cga GROUP_ID "CONTROL_ID"</c>.</summary>
    /// <param name="groupId">The group id.</param>
    /// <param name="controlId">The control to add.</param>
    /// <returns>The wire text.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="controlId"/> is null.</exception>
    public static string ChangeGroupAdd(uint groupId, string controlId)
    {
        ArgumentNullException.ThrowIfNull(controlId);
        return $"cga {groupId.ToString(CultureInfo.InvariantCulture)} {Quote(controlId)}";
    }

    /// <summary>Change Group Schedule (no-ack). Builds <c>cgsna GROUP_ID PERIOD_MS</c>.</summary>
    /// <param name="groupId">The group id.</param>
    /// <param name="periodMs">Poll period in milliseconds. 0 disables.</param>
    /// <returns>The wire text.</returns>
    public static string ChangeGroupScheduleNoAck(uint groupId, int periodMs)
        => $"cgsna {groupId.ToString(CultureInfo.InvariantCulture)} {periodMs.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>Change Group Destroy. Builds <c>cgd GROUP_ID</c>.</summary>
    /// <param name="groupId">The group id.</param>
    /// <returns>The wire text.</returns>
    public static string ChangeGroupDestroy(uint groupId)
        => $"cgd {groupId.ToString(CultureInfo.InvariantCulture)}";

    private static string Quote(string raw) => $"\"{EcpQuoting.Escape(raw)}\"";
}
