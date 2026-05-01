// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

namespace QscDspDevices.AudioControl;

/// <summary>
/// One registered preset — maps a framework-side id to a QSC Snapshot
/// Bank component code-name plus the 1..N snapshot index within it.
/// Sent to the Core as <c>Snapshot.Load { Name = bank, Bank = index }</c>
/// per <c>research/QRC_PROTOCOL.md</c> §6.1.
/// </summary>
public sealed record AudioPreset(string Id, string Bank, int Index);
