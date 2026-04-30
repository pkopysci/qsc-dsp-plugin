// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using Newtonsoft.Json;

namespace QscDspDevices.Protocol.JsonRpc;

/// <summary>
/// Outbound JSON-RPC 2.0 request as sent over the QRC wire.
/// </summary>
/// <remarks>
/// The QRC protocol does not support batched JSON-RPC; every request is
/// serialized as a single object terminated by <c>0x00</c>.
/// </remarks>
public sealed class JsonRpcRequest
{
    /// <summary>
    /// Gets the JSON-RPC protocol version literal. Always <c>"2.0"</c> on QRC.
    /// </summary>
    [JsonProperty("jsonrpc", Order = 0)]
    public string JsonRpc { get; init; } = "2.0";

    /// <summary>
    /// Gets the monotonically-increasing request id used for response
    /// correlation.
    /// </summary>
    [JsonProperty("id", Order = 1)]
    public long Id { get; init; }

    /// <summary>
    /// Gets the QRC method name, e.g. <c>"Component.Set"</c>, <c>"NoOp"</c>,
    /// <c>"ChangeGroup.AutoPoll"</c>.
    /// </summary>
    [JsonProperty("method", Order = 2)]
    public string Method { get; init; } = string.Empty;

    /// <summary>
    /// Gets the method-specific parameters. May be a JSON object, array,
    /// integer (e.g. <c>StatusGet</c>'s literal <c>0</c>), or <c>null</c>.
    /// </summary>
    [JsonProperty("params", Order = 3, NullValueHandling = NullValueHandling.Ignore)]
    public object? Params { get; init; }
}
