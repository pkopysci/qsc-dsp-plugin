// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

namespace QscDspDevices.Protocol;

/// <summary>
/// Typed enumeration of every JSON-RPC and Q-SYS-specific error code the
/// plugin classifies, per <c>research/QRC_PROTOCOL.md</c> §10.2.
/// </summary>
/// <remarks>
/// Unknown numeric codes are logged at <c>Logger.Warn</c> by the dispatcher
/// and surfaced as <see cref="ServerError"/> so callers always see a defined
/// enum value rather than a magic number.
/// </remarks>
public enum QrcErrorCode
{
    /// <summary>No error. Default state; never appears on a real error envelope.</summary>
    None = 0,

    /// <summary>JSON-RPC standard: parse error (invalid JSON received).</summary>
    ParseError = -32700,

    /// <summary>JSON-RPC standard: invalid request envelope.</summary>
    InvalidRequest = -32600,

    /// <summary>JSON-RPC standard: method not found on the server.</summary>
    MethodNotFound = -32601,

    /// <summary>JSON-RPC standard: invalid params for the called method.</summary>
    InvalidParams = -32602,

    /// <summary>JSON-RPC standard: server-side internal error.</summary>
    ServerError = -32603,

    /// <summary>
    /// QSC-specific: the Core is in Standby and rejected the command.
    /// Triggers failover-target re-evaluation on a redundant pair (M6).
    /// </summary>
    CoreOnStandby = -32604,

    /// <summary>
    /// QSC-specific: too many change groups (per-connection cap is 4).
    /// </summary>
    ChangeGroupsExhausted = 5,

    /// <summary>QSC-specific: change group id is not registered.</summary>
    UnknownChangeGroup = 6,

    /// <summary>QSC-specific: component name does not exist on the Core.</summary>
    UnknownComponentName = 7,

    /// <summary>QSC-specific: control name does not exist within the named component.</summary>
    UnknownControl = 8,

    /// <summary>QSC-specific: mixer crosspoint indices out of range.</summary>
    IllegalMixerChannelIndex = 9,

    /// <summary>QSC-specific: caller must Logon before the command is accepted.</summary>
    LogonRequired = 10,
}
