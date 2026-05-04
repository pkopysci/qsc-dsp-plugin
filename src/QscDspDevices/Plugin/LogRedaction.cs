// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using Newtonsoft.Json.Linq;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.Plugin;

/// <summary>
/// Helpers that produce a redacted, log-safe rendering of QRC payloads.
/// Used by any code path that needs to <c>Logger.Debug</c> an outbound
/// or inbound JSON-RPC request so credentials never leak into log
/// sinks. The on-wire bytes are unaffected — redaction acts on a copy.
/// </summary>
internal static class LogRedaction
{
    /// <summary>The literal placeholder substituted for any redacted value.</summary>
    public const string Placeholder = "***";

    /// <summary>
    /// Renders a JSON-RPC request as a string suitable for logging,
    /// redacting any <c>password</c>-named field in the
    /// <see cref="JsonRpcRequest.Params"/> block. Other methods pass
    /// through unchanged. Returns a stable, single-line representation.
    /// </summary>
    /// <param name="request">The request to render.</param>
    /// <returns>A log-safe string.</returns>
    public static string Render(JsonRpcRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Only Logon needs redaction today, but the helper screens any
        // request whose params block has a "password" field — future
        // QRC additions get the same treatment for free.
        if (request.Params is null)
        {
            return $"{{method={request.Method},id={request.Id}}}";
        }

        JToken cloned = JToken.FromObject(request.Params);
        if (cloned is JObject obj)
        {
            RedactPasswordField(obj);
        }

        return $"{{method={request.Method},id={request.Id},params={cloned.ToString(Newtonsoft.Json.Formatting.None)}}}";
    }

    private static void RedactPasswordField(JObject obj)
    {
        // Redact case-insensitively to cover Password / password / PASSWORD.
        foreach (JProperty property in obj.Properties())
        {
            if (string.Equals(property.Name, "password", StringComparison.OrdinalIgnoreCase))
            {
                property.Value = Placeholder;
            }
            else if (property.Value is JObject nested)
            {
                RedactPasswordField(nested);
            }
        }
    }
}
