// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using gcu_common_utils.GenericEventArgs;
using QscDspDevices.Plugin;

namespace QscDspDevices.Protocol.Ecp;

/// <summary>
/// Routes inbound ECP frames (post-framer) to subscribers. Mirrors the
/// shape of <see cref="JsonRpcDispatcher"/> with the protocol-specific
/// surface ECP needs.
/// </summary>
/// <remarks>
/// Unlike JSON-RPC, ECP has no per-message id, so the dispatcher does
/// not maintain a pending-by-id table. Every parsed response is raised
/// via <see cref="ResponseReceived"/>; subscribers filter by
/// <see cref="EcpResponseKind"/>. The QRC concept of "notification" vs
/// "response" collapses to a single event because every ECP frame is
/// effectively an asynchronous notification of state from the Core.
///
/// Malformed / unparseable frames are surfaced as
/// <see cref="EcpResponseKind.Unknown"/> with the original line in
/// <see cref="EcpResponse.Raw"/>; callers typically log
/// <c>Logger.Warn</c> and continue.
///
/// NOT thread-safe across <see cref="Dispatch(string)"/> and
/// subscriber registration: the receive loop owns dispatch; subscribers
/// register at construction time.
/// </remarks>
internal sealed class EcpDispatcher
{
    private readonly string _deviceId;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpDispatcher"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id (for log messages).</param>
    /// <exception cref="ArgumentNullException">If <paramref name="deviceId"/> is null.</exception>
    public EcpDispatcher(string deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        _deviceId = deviceId;
    }

    /// <summary>
    /// Raised once per inbound frame after parsing. Every frame
    /// produces exactly one event, including malformed inputs (which
    /// surface as <see cref="EcpResponseKind.Unknown"/>).
    /// </summary>
    public event EventHandler<GenericSingleEventArgs<EcpResponse>>? ResponseReceived;

    /// <summary>
    /// Parses and dispatches a single ECP frame. Always raises
    /// <see cref="ResponseReceived"/> exactly once.
    /// </summary>
    /// <param name="frame">The raw frame text from <see cref="EcpFramer"/> (CR already stripped).</param>
    /// <exception cref="ArgumentNullException">If <paramref name="frame"/> is null.</exception>
    public void Dispatch(string frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        EcpResponse parsed = EcpResponse.Parse(frame);
        if (parsed.Kind == EcpResponseKind.Unknown)
        {
            Log.Warn(_deviceId, $"ECP frame did not match any known response shape: '{Truncate(frame, 200)}'");
        }

        ResponseReceived?.Invoke(this, new GenericSingleEventArgs<EcpResponse>(parsed));
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";
}
