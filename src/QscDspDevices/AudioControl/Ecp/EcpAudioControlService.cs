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

        double deviceValue = _scaler.ToDevice(level, channel!.LevelMin, channel.LevelMax, channelId);
        _queue.TryEnqueue(EcpCommand.ControlSetValue(channel.LevelTag, deviceValue));

        int clamped = Math.Clamp(level, LevelScaler.FrameworkMin, LevelScaler.FrameworkMax);
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
