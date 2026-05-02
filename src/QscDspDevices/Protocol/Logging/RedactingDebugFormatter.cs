// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.Protocol.Logging;

/// <summary>
/// Formats a <see cref="JsonRpcRequest"/> for the framer's
/// <c>Logger.Debug</c> path with the <c>Password</c> field of any
/// <c>Logon</c> request replaced by <c>"***"</c>. Other request
/// methods are formatted verbatim.
/// </summary>
/// <remarks>
/// <para>
/// The redaction is purely for the debug-log path. The wire-format
/// encoding (<see cref="QrcFramer.Encode(string)"/>) is unaffected
/// — the real password still flows to the Core. This is by design:
/// production <c>Logger.Notice / Warn / Error</c> paths never log
/// full request bodies (only method names and error codes), and
/// <c>Logger.Debug</c> is off by default at runtime; turning it on
/// for diagnostics must NOT expose credentials to the log stream.
/// </para>
/// <para>
/// Pass-2 of the M2 critic flagged the missing redaction (M2 task
/// 5.3, deferred to M3). This is the M3 implementation.
/// </para>
/// </remarks>
public static class RedactingDebugFormatter
{
    /// <summary>The redacted password placeholder.</summary>
    public const string Redaction = "***";

    /// <summary>
    /// Returns a JSON-encoded string of the request suitable for
    /// <c>Logger.Debug</c>. For <c>Logon</c> requests, the
    /// <c>Password</c> field of <c>params</c> is replaced by
    /// <see cref="Redaction"/> before serialization. For other
    /// methods, the request is serialized verbatim.
    /// </summary>
    /// <param name="request">The outbound JSON-RPC request.</param>
    /// <returns>A JSON string safe to log at debug verbosity.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="request"/> is null.</exception>
    public static string Format(JsonRpcRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.Equals(request.Method, "Logon", StringComparison.Ordinal))
        {
            return JsonConvert.SerializeObject(request);
        }

        // Build a redacted shadow of the request and serialize that.
        // Constructing a new JsonRpcRequest is allocation-cheap; it
        // also keeps the redaction localized to this code path
        // without mutating the original (which is still in flight on
        // the send pipeline).
        JToken? originalParams = request.Params is null
            ? null
            : JToken.FromObject(request.Params);

        JToken? redactedParams = originalParams switch
        {
            JObject obj => RedactPassword(obj),
            JToken t => t,
            _ => null,
        };

        var redacted = new JsonRpcRequest
        {
            Id = request.Id,
            Method = request.Method,
            Params = redactedParams,
        };

        return JsonConvert.SerializeObject(redacted);
    }

    private static JObject RedactPassword(JObject original)
    {
        // Clone to avoid mutating the in-flight request's params object.
        var clone = (JObject)original.DeepClone();
        if (clone["Password"] is not null)
        {
            clone["Password"] = Redaction;
        }

        // Redact a lower-case variant for safety; QSC's docs use
        // "Password" but reference clients have shipped both casings.
        if (clone["password"] is not null)
        {
            clone["password"] = Redaction;
        }

        return clone;
    }
}
