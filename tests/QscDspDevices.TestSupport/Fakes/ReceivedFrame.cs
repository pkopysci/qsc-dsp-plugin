// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using Newtonsoft.Json.Linq;

namespace QscDspDevices.TestSupport.Fakes;

/// <summary>
/// One received JSON-RPC request as captured by
/// <see cref="FakeQrcServer.GetReceivedFrames"/>.
/// </summary>
/// <param name="Method">The QRC method name.</param>
/// <param name="Params">The params payload, if any.</param>
/// <param name="Id">The request id, if any.</param>
public sealed record ReceivedFrame(string Method, JToken? Params, long? Id);
