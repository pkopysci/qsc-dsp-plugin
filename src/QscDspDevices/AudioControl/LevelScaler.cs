// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Concurrent;
using QscDspDevices.Plugin;

namespace QscDspDevices.AudioControl;

/// <summary>
/// Pure conversion between the framework's 0–100 integer level surface
/// and the device-native range registered per channel via
/// <c>levelMin</c> / <c>levelMax</c>.
/// </summary>
/// <remarks>
/// <para>
/// QSC controls accept levels in heterogeneous units — dB
/// (typically <c>-100..0</c> or <c>-80..20</c>), normalised float
/// (<c>0.0..1.0</c>), or device-defined integer counts (e.g.
/// <c>0..50</c> mixer steps). The framework reports the per-channel
/// native range to us once at registration time; this scaler applies
/// it on every wire write and AutoPoll-driven cache update.
/// </para>
/// <para>
/// Rounding is half-up (not the .NET default banker's half-to-even),
/// because the QSC Core does the same when displaying scaled values
/// in Designer; matching avoids a "set 50, see 49" UX paper-cut.
/// </para>
/// <para>
/// Out-of-range framework input clamps to the boundary and logs
/// <c>Logger.Warn</c> once per offending channel id (subsequent
/// out-of-range calls for the same id MUST NOT re-log; framework
/// callers occasionally drive levels in tight loops and we don't
/// want to flood the log).
/// </para>
/// </remarks>
public sealed class LevelScaler
{
    /// <summary>The framework's minimum level surface value (0–100).</summary>
    public const int FrameworkMin = 0;

    /// <summary>The framework's maximum level surface value (0–100).</summary>
    public const int FrameworkMax = 100;

    private readonly string _deviceId;
    private readonly ConcurrentDictionary<string, byte> _warnedIds = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelScaler"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id, used in log messages.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="deviceId"/> is null.</exception>
    public LevelScaler(string deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        _deviceId = deviceId;
    }

    /// <summary>
    /// Converts a device-native value back to the framework 0–100
    /// surface using half-up rounding. Inputs outside
    /// <c>[min, max]</c> clamp before scaling (the device should
    /// not report out-of-range values, but we belt-and-brace).
    /// </summary>
    /// <param name="deviceValue">The device-native value.</param>
    /// <param name="min">The device-native minimum.</param>
    /// <param name="max">The device-native maximum.</param>
    /// <returns>The framework 0–100 level.</returns>
    /// <exception cref="ArgumentException">If <paramref name="min"/> is greater than or equal to <paramref name="max"/>.</exception>
    public static int ToFramework(double deviceValue, int min, int max)
    {
        if (min >= max)
        {
            throw new ArgumentException($"min ({min}) must be less than max ({max}).", nameof(min));
        }

        double clamped = deviceValue;
        if (deviceValue < min)
        {
            clamped = min;
        }
        else if (deviceValue > max)
        {
            clamped = max;
        }

        double fraction = (clamped - min) / (max - min);
        double rawFramework = FrameworkMin + (fraction * (FrameworkMax - FrameworkMin));

        // Half-up rounding: 0.5 → 1, -0.5 → 0 (we never see negative
        // here since fraction ∈ [0,1]). Math.Round defaults to
        // banker's rounding (half-to-even) which would map 0.5 → 0
        // and 1.5 → 2 — symmetric, but inconsistent with how the
        // QSC Core's Designer UI displays scaled values.
        return (int)Math.Floor(rawFramework + 0.5);
    }

    /// <summary>
    /// Converts a framework 0–100 level to the device-native range,
    /// clamping out-of-range inputs and logging a one-shot warning
    /// per offending <paramref name="channelId"/>.
    /// </summary>
    /// <param name="frameworkLevel">The 0–100 level.</param>
    /// <param name="min">The device-native minimum.</param>
    /// <param name="max">The device-native maximum.</param>
    /// <param name="channelId">The owning channel id, used for one-shot warn dedup.</param>
    /// <returns>The device-native value (clamped if out of range).</returns>
    /// <exception cref="ArgumentException">If <paramref name="min"/> is greater than or equal to <paramref name="max"/>.</exception>
    public double ToDevice(int frameworkLevel, int min, int max, string channelId)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        if (min >= max)
        {
            throw new ArgumentException($"min ({min}) must be less than max ({max}).", nameof(min));
        }

        int clamped = frameworkLevel;
        if (frameworkLevel < FrameworkMin)
        {
            clamped = FrameworkMin;
            WarnOutOfRangeOnce(channelId, frameworkLevel);
        }
        else if (frameworkLevel > FrameworkMax)
        {
            clamped = FrameworkMax;
            WarnOutOfRangeOnce(channelId, frameworkLevel);
        }

        double fraction = (clamped - FrameworkMin) / (double)(FrameworkMax - FrameworkMin);
        return min + (fraction * (max - min));
    }

    private void WarnOutOfRangeOnce(string channelId, int frameworkLevel)
    {
        if (_warnedIds.TryAdd(channelId, 0))
        {
            Log.Warn(
                _deviceId,
                $"Level {frameworkLevel} for channel '{channelId}' is outside the framework 0–100 range; clamping. Subsequent out-of-range writes for this id will not re-log.");
        }
    }
}
