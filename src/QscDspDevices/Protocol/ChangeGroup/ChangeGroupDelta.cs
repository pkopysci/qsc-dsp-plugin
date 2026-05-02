// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using Newtonsoft.Json.Linq;

namespace QscDspDevices.Protocol.ChangeGroup;

/// <summary>
/// One entry from a <c>ChangeGroup.Poll</c> or AutoPoll response's
/// <c>Changes</c> array. The <c>Value</c> is kept as a <see cref="JToken"/>
/// because QRC controls have heterogeneous types — booleans for mute,
/// doubles for level, strings for text controls — and the change-group
/// manager forwards the token to the per-feature consumer (M3 audio,
/// M4 routing) which knows how to interpret it.
/// </summary>
public sealed record ChangeGroupDelta(string Name, JToken Value, JToken? StringValue, double? Position);
