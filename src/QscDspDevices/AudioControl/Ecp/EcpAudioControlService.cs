// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Concurrent;
using gcu_common_utils.GenericEventArgs;
using QscDspDevices.Connectivity.Ecp;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol.Ecp;

namespace QscDspDevices.AudioControl.Ecp;

/// <summary>
/// ECP-side audio-control service. Translates framework Set/Get
/// operations into ECP <c>csv</c> / <c>css</c> wire commands and
/// maintains an optimistic cache of the framework-side level / mute.
/// Per design.md §D-E1, this is a parallel service rather than a
/// translation layer over <see cref="AudioControlService"/>.
/// </summary>
internal sealed class EcpAudioControlService
{
    private readonly string _deviceId;
    private readonly AudioChannelRegistry _registry;
    private readonly LevelScaler _scaler;
    private readonly EcpCommandQueue _queue;
    private readonly ConcurrentDictionary<string, int> _levelCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, bool> _muteCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpAudioControlService"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="registry">The audio-channel registry.</param>
    /// <param name="scaler">The level scaler shared with the QRC service.</param>
    /// <param name="queue">The ECP command queue.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public EcpAudioControlService(string deviceId, AudioChannelRegistry registry, LevelScaler scaler, EcpCommandQueue queue)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(scaler);
        ArgumentNullException.ThrowIfNull(queue);

        _deviceId = deviceId;
        _registry = registry;
        _scaler = scaler;
        _queue = queue;
    }

    /// <summary>Raised on input-channel level changes.</summary>
    public event EventHandler<GenericDualEventArgs<string, string>>? AudioInputLevelChanged;

    /// <summary>Raised on input-channel mute changes.</summary>
    public event EventHandler<GenericDualEventArgs<string, string>>? AudioInputMuteChanged;

    /// <summary>Raised on output-channel level changes.</summary>
    public event EventHandler<GenericDualEventArgs<string, string>>? AudioOutputLevelChanged;

    /// <summary>Raised on output-channel mute changes.</summary>
    public event EventHandler<GenericDualEventArgs<string, string>>? AudioOutputMuteChanged;

    /// <summary>Sets the level on the named control via <c>csv</c>.</summary>
    /// <param name="channelId">The framework channel id.</param>
    /// <param name="level">0–100 framework level.</param>
    public void SetLevel(string channelId, int level)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        if (!_registry.TryGetChannel(channelId, out AudioChannel? channel))
        {
            Log.Error(_deviceId, $"ECP SetLevel called with unknown channel id '{channelId}'.");
            return;
        }

        // Issue #24: send csp (set position) so the Core maps the
        // framework's 0–100 to the design's configured range. This
        // bypasses the LevelMin/LevelMax both-zero failure mode and
        // matches the QRC service tier's M-fix to use Position.
        int clamped = Math.Clamp(level, LevelScaler.FrameworkMin, LevelScaler.FrameworkMax);
        double position = (double)(clamped - LevelScaler.FrameworkMin) / (LevelScaler.FrameworkMax - LevelScaler.FrameworkMin);
        _queue.TryEnqueue(EcpCommand.ControlSetPosition(channel!.LevelTag, position));
        if (_levelCache.TryGetValue(channelId, out int prior) && prior == clamped)
        {
            return;
        }

        _levelCache[channelId] = clamped;
        var args = new GenericDualEventArgs<string, string>(channelId, clamped.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (channel.IsInput)
        {
            AudioInputLevelChanged?.Invoke(this, args);
        }
        else
        {
            AudioOutputLevelChanged?.Invoke(this, args);
        }
    }

    /// <summary>Sets mute on the named control via <c>css</c>.</summary>
    /// <param name="channelId">The framework channel id.</param>
    /// <param name="mute">The new mute state.</param>
    public void SetMute(string channelId, bool mute)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        if (!_registry.TryGetChannel(channelId, out AudioChannel? channel))
        {
            Log.Error(_deviceId, $"ECP SetMute called with unknown channel id '{channelId}'.");
            return;
        }

        _queue.TryEnqueue(EcpCommand.ControlSetString(channel!.MuteTag, mute ? "true" : "false"));

        if (_muteCache.TryGetValue(channelId, out bool prior) && prior == mute)
        {
            return;
        }

        _muteCache[channelId] = mute;
        var args = new GenericDualEventArgs<string, string>(channelId, mute ? "true" : "false");
        if (channel.IsInput)
        {
            AudioInputMuteChanged?.Invoke(this, args);
        }
        else
        {
            AudioOutputMuteChanged?.Invoke(this, args);
        }
    }

    /// <summary>Returns the cached level for a channel id (0 if unknown).</summary>
    /// <param name="channelId">The framework channel id.</param>
    /// <returns>The cached 0–100 level.</returns>
    public int GetLevel(string channelId)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        return _levelCache.TryGetValue(channelId, out int level) ? level : 0;
    }

    /// <summary>Returns the cached mute for a channel id (false if unknown).</summary>
    /// <param name="channelId">The framework channel id.</param>
    /// <returns>The cached mute state.</returns>
    public bool GetMute(string channelId)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        return _muteCache.TryGetValue(channelId, out bool mute) && mute;
    }

    /// <summary>
    /// Reconciles the optimistic level cache against an inbound
    /// <c>cv</c> from the AutoPoll bridge. Prefers the Core-supplied
    /// <paramref name="position"/> (already in 0–1 normalized to the
    /// design's configured range) when it's a sensible value;
    /// otherwise scales <paramref name="rawValue"/> through the
    /// channel's LevelMin/LevelMax. The cache is corrected and the
    /// matching <c>AudioInput/OutputLevelChanged</c> event re-fires.
    /// </summary>
    /// <param name="channel">The registered channel matching the inbound tag.</param>
    /// <param name="rawValue">The numeric value from the <c>cv</c> response.</param>
    /// <param name="position">The position (0–1) field from the <c>cv</c> response.</param>
    public void OnInboundLevel(AudioChannel channel, double rawValue, double position)
    {
        ArgumentNullException.ThrowIfNull(channel);

        // Prefer position (0–1, design-normalized by the Core) when
        // it's in range; this is robust to LevelMin/LevelMax being
        // unset (issue #24). Fall back to value-with-scaler otherwise.
        int frameworkValue;
        if (position is >= 0 and <= 1)
        {
            frameworkValue = (int)Math.Round((position * (LevelScaler.FrameworkMax - LevelScaler.FrameworkMin)) + LevelScaler.FrameworkMin);
        }
        else
        {
            frameworkValue = LevelScaler.ToFramework(rawValue, channel.LevelMin, channel.LevelMax);
        }

        if (_levelCache.TryGetValue(channel.Id, out int prior) && prior == frameworkValue)
        {
            return;
        }

        _levelCache[channel.Id] = frameworkValue;
        var args = new GenericDualEventArgs<string, string>(channel.Id, frameworkValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (channel.IsInput)
        {
            AudioInputLevelChanged?.Invoke(this, args);
        }
        else
        {
            AudioOutputLevelChanged?.Invoke(this, args);
        }
    }

    /// <summary>
    /// Reconciles the optimistic mute cache against an inbound
    /// <c>cv</c> on the channel's mute tag.
    /// </summary>
    /// <param name="channel">The registered channel matching the inbound tag.</param>
    /// <param name="muted">True if the Core reports the control muted.</param>
    public void OnInboundMute(AudioChannel channel, bool muted)
    {
        ArgumentNullException.ThrowIfNull(channel);
        if (_muteCache.TryGetValue(channel.Id, out bool prior) && prior == muted)
        {
            return;
        }

        _muteCache[channel.Id] = muted;
        var args = new GenericDualEventArgs<string, string>(channel.Id, muted ? "true" : "false");
        if (channel.IsInput)
        {
            AudioInputMuteChanged?.Invoke(this, args);
        }
        else
        {
            AudioOutputMuteChanged?.Invoke(this, args);
        }
    }

    /// <summary>Recalls a preset by emitting <c>ssl</c>.</summary>
    /// <param name="presetId">The preset id (registered with the channel registry).</param>
    public void RecallPreset(string presetId)
    {
        ArgumentNullException.ThrowIfNull(presetId);
        if (!_registry.TryGetPreset(presetId, out AudioPreset? preset))
        {
            Log.Error(_deviceId, $"ECP RecallPreset called with unknown preset id '{presetId}'.");
            return;
        }

        _queue.TryEnqueue(EcpCommand.SnapshotLoad(preset!.Bank, preset.Index));
    }
}
