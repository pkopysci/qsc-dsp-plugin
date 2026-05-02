// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Concurrent;
using gcu_common_utils.GenericEventArgs;
using Newtonsoft.Json.Linq;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.AudioControl;

/// <summary>
/// Orchestrates the framework-side <c>IAudioControl</c> surface against
/// the QRC wire: sends <c>Control.Set</c> for outbound writes, holds
/// the per-channel cache for synchronous <c>Get*</c> reads, and raises
/// the four <c>AudioInput*Changed</c> / <c>AudioOutput*Changed</c>
/// events when AutoPoll deltas mutate the cache.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cache semantics — intent, not state.</b> <c>Set*</c> updates the
/// in-process cache <i>before</i> attempting <c>TryEnqueue</c>. The
/// cache therefore tracks the framework's most recent <i>intent</i>,
/// not the Core's confirmed state. While disconnected, <c>Set*</c>
/// records the intent and the queue silently refuses the wire write
/// (M2 contract: queue refuses while disconnected). On reconnect, the
/// hydration <c>ChangeGroup.AutoPoll</c> response replays the Core's
/// real values and reconciles the cache — the framework will see one
/// batch of <c>*Changed</c> events for any channel whose cached intent
/// drifted from the Core's actual state. This is the documented and
/// intentional shape; the alternative (refuse Set while disconnected)
/// would force the framework's audio scenes into a per-call connection
/// check, which the IAudioControl surface does not expose.
/// </para>
/// </remarks>
public sealed class AudioControlService
{
    private readonly string _deviceId;
    private readonly AudioChannelRegistry _registry;
    private readonly LevelScaler _scaler;
    private readonly CommandQueue _queue;
    private readonly IdGenerator _ids;

    private readonly ConcurrentDictionary<string, int> _levelCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, bool> _muteCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioControlService"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="registry">The channel registry.</param>
    /// <param name="scaler">The level scaler shared with this service.</param>
    /// <param name="queue">The command queue requests are enqueued on.</param>
    /// <param name="ids">The shared monotonic id generator.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public AudioControlService(
        string deviceId,
        AudioChannelRegistry registry,
        LevelScaler scaler,
        CommandQueue queue,
        IdGenerator ids)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(scaler);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(ids);

        _deviceId = deviceId;
        _registry = registry;
        _scaler = scaler;
        _queue = queue;
        _ids = ids;
    }

    /// <summary>Raised on transition of an input channel's cached level.</summary>
    public event EventHandler<GenericDualEventArgs<string, string>>? AudioInputLevelChanged;

    /// <summary>Raised on transition of an input channel's cached mute state.</summary>
    public event EventHandler<GenericDualEventArgs<string, string>>? AudioInputMuteChanged;

    /// <summary>Raised on transition of an output channel's cached level.</summary>
    public event EventHandler<GenericDualEventArgs<string, string>>? AudioOutputLevelChanged;

    /// <summary>Raised on transition of an output channel's cached mute state.</summary>
    public event EventHandler<GenericDualEventArgs<string, string>>? AudioOutputMuteChanged;

    /// <summary>
    /// Implements <c>SetAudioInputLevel</c> / <c>SetAudioOutputLevel</c>.
    /// Validates the channel id, scales 0–100 → device-native, enqueues
    /// <c>Control.Set</c>, and updates the cache optimistically.
    /// </summary>
    /// <param name="channelId">The framework channel id.</param>
    /// <param name="level">The 0–100 level.</param>
    public void SetLevel(string channelId, int level)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        if (!_registry.TryGetChannel(channelId, out AudioChannel? channel))
        {
            Log.Error(_deviceId, $"SetLevel called with unknown channel id '{channelId}'.");
            return;
        }

        double deviceValue = _scaler.ToDevice(level, channel!.LevelMin, channel.LevelMax, channelId);
        var request = new JsonRpcRequest
        {
            Id = _ids.Next(),
            Method = "Control.Set",
            Params = new { Name = channel.LevelTag, Value = deviceValue },
        };

        // Optimistic-update: record the framework-side level so a subsequent
        // Get* returns it without waiting on the AutoPoll round-trip. The
        // cache update runs regardless of whether the queue accepted the
        // request (it refuses while disconnected per M2 design); the Set
        // surface is the framework's authoritative intent and Get* must
        // reflect it. The next AutoPoll after reconnect reconciles cache
        // with the Core's actual state.
        UpdateLevelCacheAndRaise(channel, Math.Clamp(level, LevelScaler.FrameworkMin, LevelScaler.FrameworkMax));

        // CommandQueue.TryEnqueue logs on its own when refusing; we do not
        // re-log here.
        _queue.TryEnqueue(request);
    }

    /// <summary>
    /// Implements <c>SetAudioInputMute</c> / <c>SetAudioOutputMute</c>.
    /// </summary>
    /// <param name="channelId">The framework channel id.</param>
    /// <param name="mute">The new mute state.</param>
    public void SetMute(string channelId, bool mute)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        if (!_registry.TryGetChannel(channelId, out AudioChannel? channel))
        {
            Log.Error(_deviceId, $"SetMute called with unknown channel id '{channelId}'.");
            return;
        }

        var request = new JsonRpcRequest
        {
            Id = _ids.Next(),
            Method = "Control.Set",
            Params = new { Name = channel!.MuteTag, Value = mute },
        };

        // See SetLevel — cache update is unconditional on Set; queue-accept
        // is best-effort.
        UpdateMuteCacheAndRaise(channel, mute);
        _queue.TryEnqueue(request);
    }

    /// <summary>
    /// Implements <c>GetAudioInputLevel</c> / <c>GetAudioOutputLevel</c>.
    /// Returns 0 for unknown ids (per the framework spec).
    /// </summary>
    /// <param name="channelId">The framework channel id.</param>
    /// <returns>The cached 0–100 level.</returns>
    public int GetLevel(string channelId)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        return _levelCache.TryGetValue(channelId, out int level) ? level : 0;
    }

    /// <summary>
    /// Implements <c>GetAudioInputMute</c> / <c>GetAudioOutputMute</c>.
    /// Returns false for unknown ids.
    /// </summary>
    /// <param name="channelId">The framework channel id.</param>
    /// <returns>The cached mute state.</returns>
    public bool GetMute(string channelId)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        return _muteCache.TryGetValue(channelId, out bool mute) && mute;
    }

    /// <summary>
    /// AutoPoll delta callback. Routes a single delta to the cache,
    /// raising the matching event when the value changed. This is the
    /// callback registered with <c>ChangeGroupManager.SetDeltaCallback</c>.
    /// </summary>
    /// <param name="delta">The parsed delta.</param>
    public void OnDeviceUpdate(ChangeGroupDelta delta)
    {
        ArgumentNullException.ThrowIfNull(delta);

        if (!_registry.TryGetChannelIdByTag(delta.Name, out string? channelId)
            || channelId is null
            || !_registry.TryGetChannel(channelId, out AudioChannel? channel)
            || channel is null)
        {
            // Unknown tag — could be a control we registered for a higher
            // milestone (M4 routing, M5 logic) that is not yet implemented.
            // Don't log at error; this is expected during incremental rollout.
            return;
        }

        bool isLevel = string.Equals(delta.Name, channel.LevelTag, StringComparison.Ordinal);
        if (isLevel)
        {
            int frameworkValue = ExtractLevel(delta);
            UpdateLevelCacheAndRaise(channel, frameworkValue);
        }
        else if (string.Equals(delta.Name, channel.MuteTag, StringComparison.Ordinal))
        {
            bool mute = ExtractMute(delta);
            UpdateMuteCacheAndRaise(channel, mute);
        }
    }

    private static bool ExtractMute(ChangeGroupDelta delta)
    {
        // QSC mute is reported as boolean in `Value`. Older firmware paths
        // sometimes report it as 0/1 numeric — accept both.
        return delta.Value.Type switch
        {
            JTokenType.Boolean => delta.Value.ToObject<bool>(),
            JTokenType.Integer => delta.Value.ToObject<int>() != 0,
            JTokenType.Float => Math.Abs(delta.Value.ToObject<double>()) > double.Epsilon,
            _ => false,
        };
    }

    private int ExtractLevel(ChangeGroupDelta delta)
    {
        // Prefer `Position` (0..1 normalized) when present — it's already
        // in framework-friendly range and bypasses the per-channel min/max
        // scaling. Fall back to `Value` interpreted via the scaler.
        if (delta.Position.HasValue)
        {
            double pos = Math.Clamp(delta.Position.Value, 0.0, 1.0);
            return (int)Math.Floor((pos * (LevelScaler.FrameworkMax - LevelScaler.FrameworkMin)) + LevelScaler.FrameworkMin + 0.5);
        }

        if (delta.Value.Type is JTokenType.Float or JTokenType.Integer)
        {
            // We don't know which channel this delta is for at this point;
            // resolve via the registry.
            if (_registry.TryGetChannelIdByTag(delta.Name, out string? channelId)
                && channelId is not null
                && _registry.TryGetChannel(channelId, out AudioChannel? channel)
                && channel is not null)
            {
                double native = delta.Value.ToObject<double>();
                return LevelScaler.ToFramework(native, channel.LevelMin, channel.LevelMax);
            }
        }

        return 0;
    }

    private void UpdateLevelCacheAndRaise(AudioChannel channel, int newLevel)
    {
        bool changed = !_levelCache.TryGetValue(channel.Id, out int prior) || prior != newLevel;
        _levelCache[channel.Id] = newLevel;

        if (!changed)
        {
            return;
        }

        var args = new GenericDualEventArgs<string, string>(_deviceId, channel.Id);
        if (channel.IsInput)
        {
            AudioInputLevelChanged?.Invoke(this, args);
        }
        else
        {
            AudioOutputLevelChanged?.Invoke(this, args);
        }
    }

    private void UpdateMuteCacheAndRaise(AudioChannel channel, bool newMute)
    {
        bool changed = !_muteCache.TryGetValue(channel.Id, out bool prior) || prior != newMute;
        _muteCache[channel.Id] = newMute;

        if (!changed)
        {
            return;
        }

        var args = new GenericDualEventArgs<string, string>(_deviceId, channel.Id);
        if (channel.IsInput)
        {
            AudioInputMuteChanged?.Invoke(this, args);
        }
        else
        {
            AudioOutputMuteChanged?.Invoke(this, args);
        }
    }
}
