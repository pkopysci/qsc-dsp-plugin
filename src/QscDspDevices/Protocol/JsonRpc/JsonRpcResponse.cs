// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QscDspDevices.Protocol.JsonRpc;

/// <summary>
/// Inbound JSON-RPC 2.0 response or notification as received from the
/// Q-SYS Core. May represent a successful response (<see cref="Result"/>
/// populated), an error response (<see cref="Error"/> populated), or a
/// server-originated notification (<see cref="Method"/> populated and
/// <see cref="Id"/> absent).
/// </summary>
public sealed class JsonRpcResponse
{
    /// <summary>
    /// Gets the JSON-RPC protocol version literal. Always <c>"2.0"</c> on QRC.
    /// </summary>
    [JsonProperty("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    /// <summary>
    /// Gets the id of the request this response correlates to. Absent for
    /// server-originated notifications.
    /// </summary>
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public long? Id { get; init; }

    /// <summary>
    /// Gets the method name when this is a server-originated notification or
    /// AutoPoll-pushed update (e.g. <c>"EngineStatus"</c>).
    /// </summary>
    [JsonProperty("method", NullValueHandling = NullValueHandling.Ignore)]
    public string? Method { get; init; }

    /// <summary>
    /// Gets the server's success result. Populated when the request succeeded.
    /// </summary>
    [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
    public JToken? Result { get; init; }

    /// <summary>
    /// Gets the server's params block. Populated for server-originated
    /// notifications and AutoPoll pushes.
    /// </summary>
    [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
    public JToken? Params { get; init; }

    /// <summary>
    /// Gets the server's error envelope. Populated when the request failed.
    /// </summary>
    [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
    public JsonRpcError? Error { get; init; }

    /// <summary>
    /// Gets a value indicating whether this response represents a server-
    /// originated notification (no <see cref="Id"/>, populated <see cref="Method"/>).
    /// </summary>
    [JsonIgnore]
    public bool IsNotification => Id is null && !string.IsNullOrEmpty(Method);

    /// <summary>
    /// Gets a value indicating whether this response represents an error.
    /// </summary>
    [JsonIgnore]
    public bool IsError => Error is not null;
}
