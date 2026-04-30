// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QscDspDevices.Protocol.JsonRpc;

/// <summary>
/// JSON-RPC 2.0 error envelope as received from the Q-SYS Core.
/// </summary>
/// <remarks>
/// Standard JSON-RPC codes (<c>-32700</c> through <c>-32603</c>) and
/// QSC-specific codes (<c>-32604</c> CoreOnStandby, plus <c>5</c>..<c>10</c>)
/// are mapped to <see cref="QrcErrorCode"/> by the dispatcher.
/// </remarks>
public sealed class JsonRpcError
{
    /// <summary>Gets the numeric error code.</summary>
    [JsonProperty("code")]
    public int Code
    {
        get; init;
    }

    /// <summary>Gets the human-readable error message.</summary>
    [JsonProperty("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional opaque diagnostic data. The QRC docs do not formalize
    /// the shape; treat as a JSON token to be logged or surfaced verbatim.
    /// </summary>
    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    public JToken? Data
    {
        get; init;
    }
}
