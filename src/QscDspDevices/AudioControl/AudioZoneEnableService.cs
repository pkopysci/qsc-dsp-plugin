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
/// Orchestrates the framework-side <c>IAudioZoneEnabler</c> surface.
/// Sends <c>Control.Set</c> on the registered <c>controlTag</c> for
/// <c>Set</c> / <c>Toggle</c>, serves <c>Query</c> from the cache,
/// and raises <c>AudioZoneEnableChanged</c> with the
/// <c>(channelId, zoneId)</c> args the framework spec requires.
/// </summary>
public sealed class AudioZoneEnableService
{
    private readonly string _deviceId;
    private readonly AudioZoneRegistry _registry;
    private readonly CommandQueue _queue;
    private readonly IdGenerator _ids;

    private readonly ConcurrentDictionary<(string ChannelId, string ZoneId), bool> _cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioZoneEnableService"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="registry">The zone registry.</param>
    /// <param name="queue">The command queue requests are enqueued on.</param>
    /// <param name="ids">The shared monotonic id generator.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public AudioZoneEnableService(string deviceId, AudioZoneRegistry registry, CommandQueue queue, IdGenerator ids)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(ids);

        _deviceId = deviceId;
        _registry = registry;
        _queue = queue;
        _ids = ids;
    }

    /// <summary>Raised when a registered pair's cached enable state transitions.</summary>
    public event EventHandler<GenericDualEventArgs<string, string>>? ZoneEnableChanged;

    /// <summary>
    /// Implements <c>IAudioZoneEnabler.SetAudioZoneEnable</c>.
    /// Unknown pair logs <c>Logger.Error</c> and is a silent no-op.
    /// </summary>
    /// <param name="channelId">The owning input channel id.</param>
    /// <param name="zoneId">The zone id.</param>
    /// <param name="enable">The new enable state.</param>
    public void Set(string channelId, string zoneId, bool enable)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        ArgumentNullException.ThrowIfNull(zoneId);

        if (!_registry.TryGet(channelId, zoneId, out string? controlTag) || controlTag is null)
        {
            Log.Error(_deviceId, $"SetAudioZoneEnable called with unknown pair ({channelId}, {zoneId}).");
            return;
        }

        var request = new JsonRpcRequest
        {
            Id = _ids.Next(),
            Method = "Control.Set",
            Params = new { Name = controlTag, Value = enable },
        };

        UpdateCacheAndRaise(channelId, zoneId, enable);
        _queue.TryEnqueue(request);
    }

    /// <summary>
    /// Implements <c>IAudioZoneEnabler.ToggleAudioZoneEnable</c>.
    /// Reads the cached value (default <c>false</c> for never-seen
    /// pairs), sends <c>Control.Set</c> with the inverted value, and
    /// updates the cache. Unknown pair is a silent no-op.
    /// </summary>
    /// <param name="channelId">The owning input channel id.</param>
    /// <param name="zoneId">The zone id.</param>
    public void Toggle(string channelId, string zoneId)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        ArgumentNullException.ThrowIfNull(zoneId);

        if (!_registry.TryGet(channelId, zoneId, out string? controlTag) || controlTag is null)
        {
            Log.Error(_deviceId, $"ToggleAudioZoneEnable called with unknown pair ({channelId}, {zoneId}).");
            return;
        }

        bool current = _cache.TryGetValue((channelId, zoneId), out bool cached) && cached;
        bool next = !current;

        var request = new JsonRpcRequest
        {
            Id = _ids.Next(),
            Method = "Control.Set",
            Params = new { Name = controlTag, Value = next },
        };

        UpdateCacheAndRaise(channelId, zoneId, next);
        _queue.TryEnqueue(request);
    }

    /// <summary>
    /// Implements <c>IAudioZoneEnabler.QueryAudioZoneEnable</c>. Returns
    /// the cached value; <c>false</c> for unknown or never-updated pairs.
    /// </summary>
    /// <param name="channelId">The owning input channel id.</param>
    /// <param name="zoneId">The zone id.</param>
    /// <returns>The cached enable state.</returns>
    public bool Query(string channelId, string zoneId)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        ArgumentNullException.ThrowIfNull(zoneId);
        return _cache.TryGetValue((channelId, zoneId), out bool cached) && cached;
    }

    /// <summary>
    /// AutoPoll delta callback. The fan-out dispatcher routes zone-
    /// controlTag deltas here; other tags should be filtered upstream.
    /// </summary>
    /// <param name="delta">The parsed delta.</param>
    public void OnDeviceUpdate(ChangeGroupDelta delta)
    {
        ArgumentNullException.ThrowIfNull(delta);

        if (!_registry.TryGetPair(delta.Name, out (string ChannelId, string ZoneId) pair))
        {
            return;
        }

        bool enable = ExtractBoolean(delta);
        UpdateCacheAndRaise(pair.ChannelId, pair.ZoneId, enable);
    }

    private static bool ExtractBoolean(ChangeGroupDelta delta)
    {
        return delta.Value.Type switch
        {
            JTokenType.Boolean => delta.Value.ToObject<bool>(),
            JTokenType.Integer => delta.Value.ToObject<int>() != 0,
            JTokenType.Float => Math.Abs(delta.Value.ToObject<double>()) > double.Epsilon,
            _ => false,
        };
    }

    private void UpdateCacheAndRaise(string channelId, string zoneId, bool newEnable)
    {
        var key = (channelId, zoneId);
        bool changed = !_cache.TryGetValue(key, out bool prior) || prior != newEnable;
        _cache[key] = newEnable;

        if (!changed)
        {
            return;
        }

        ZoneEnableChanged?.Invoke(this, new GenericDualEventArgs<string, string>(channelId, zoneId));
    }
}
