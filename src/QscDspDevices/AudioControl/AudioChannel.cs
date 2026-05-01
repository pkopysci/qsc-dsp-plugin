// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;

namespace QscDspDevices.AudioControl;

/// <summary>
/// One registered audio channel — a level + mute pair on a Q-SYS Core
/// keyed by the framework-supplied id. Captures the device-native
/// level range so <see cref="LevelScaler"/> can convert between the
/// framework's 0–100 integer surface and whatever wire-format the
/// underlying QSC control accepts (dB, normalised float, integer
/// counts; per-control).
/// </summary>
public sealed record AudioChannel(
    string Id,
    string LevelTag,
    string MuteTag,
    int LevelMin,
    int LevelMax,
    bool IsInput,
    int RouterIndex,
    int BankIndex,
    IReadOnlyList<string> Tags);
