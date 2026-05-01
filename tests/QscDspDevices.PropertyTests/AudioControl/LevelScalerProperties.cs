// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FsCheck.Xunit;
using QscDspDevices.AudioControl;

namespace QscDspDevices.PropertyTests.AudioControl;

/// <summary>
/// FsCheck property tests for <see cref="LevelScaler"/>. Pin the
/// round-trip contract that the audio-control spec requires: a
/// framework value run through ToDevice and back to ToFramework
/// must land within ±1 of the original for every legal range.
/// </summary>
public class LevelScalerProperties
{
    /// <summary>
    /// Round-trip property over the full 0–100 framework range and
    /// arbitrary device-native ranges (with min less than max).
    /// </summary>
    /// <param name="frameworkLevel">Random framework level (will be normalized to 0–100).</param>
    /// <param name="rawMin">Random device-native minimum.</param>
    /// <param name="rawSpan">Random positive span (added to min to produce max).</param>
    /// <returns>True if the round-trip is within ±1 of the original.</returns>
    [Property]
    public bool ToDevice_then_ToFramework_round_trips_within_one(
        int frameworkLevel, int rawMin, int rawSpan)
    {
        // Normalize: framework input clamps under contract, but the round-
        // trip property only holds for in-range inputs. Pin to [0,100].
        int level = Math.Abs(frameworkLevel) % 101;

        // Min and span must satisfy max > min and produce a sensible range
        // (not so tiny we lose precision).
        int min = Math.Clamp(rawMin, -1_000_000, 1_000_000);
        int span = Math.Max(1, Math.Abs(rawSpan) % 1_000_001);
        int max = min + span;

        var scaler = new LevelScaler("dsp-1");
        double device = scaler.ToDevice(level, min, max, "ch");
        int back = LevelScaler.ToFramework(device, min, max);
        return back >= level - 1 && back <= level + 1;
    }

    /// <summary>
    /// Out-of-range framework input clamps to the same device value
    /// as the corresponding in-range boundary (0 or 100).
    /// </summary>
    /// <param name="rawLevel">Random framework level (will be forced out of range).</param>
    /// <param name="rawMin">Random device-native minimum.</param>
    /// <param name="rawSpan">Random positive span.</param>
    /// <returns>True if out-of-range input matches the boundary value.</returns>
    [Property]
    public bool Out_of_range_framework_input_clamps_to_boundary(
        int rawLevel, int rawMin, int rawSpan)
    {
        // Force the input out of range. We don't care about magnitudes here —
        // any negative or any value > 100 should clamp.
        int level = rawLevel < 0 ? rawLevel : rawLevel + 101;

        int min = Math.Clamp(rawMin, -1_000_000, 1_000_000);
        int span = Math.Max(1, Math.Abs(rawSpan) % 1_000_001);
        int max = min + span;

        var scaler = new LevelScaler("dsp-1");
        double clampedDevice = scaler.ToDevice(level, min, max, "ch-test");
        double boundaryDevice = level < 0
            ? scaler.ToDevice(0, min, max, "ch-low")
            : scaler.ToDevice(100, min, max, "ch-high");
        return Math.Abs(clampedDevice - boundaryDevice) < 1e-9;
    }
}
