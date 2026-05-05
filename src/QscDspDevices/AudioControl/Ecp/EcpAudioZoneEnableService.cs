// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Concurrent;
using gcu_common_utils.GenericEventArgs;
using QscDspDevices.Connectivity.Ecp;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol.Ecp;

namespace QscDspDevices.AudioControl.Ecp;

/// <summary>
/// ECP-side audio-zone-enable service. Translates Set / Toggle / Query
/// against named zone-enable controls into <c>css</c> wire commands.
/// </summary>
internal sealed class EcpAudioZoneEnableService
{
    private readonly string _deviceId;
    private readonly AudioZoneRegistry _registry;
    private readonly EcpCommandQueue _queue;
    private readonly ConcurrentDictionary<(string, string), bool> _cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpAudioZoneEnableService"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="registry">The zone registry.</param>
    /// <param name="queue">The ECP command queue.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public EcpAudioZoneEnableService(string deviceId, AudioZoneRegistry registry, EcpCommandQueue queue)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(queue);

        _deviceId = deviceId;
        _registry = registry;
        _queue = queue;
    }

    /// <summary>Raised on transition with (channelId, zoneId).</summary>
    public event EventHandler<GenericDualEventArgs<string, string>>? ZoneEnableChanged;

    /// <summary>Sets the zone-enable boolean.</summary>
    /// <param name="channelId">The owning input channel id.</param>
    /// <param name="zoneId">The zone id.</param>
    /// <param name="enable">The new state.</param>
    public void Set(string channelId, string zoneId, bool enable)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        ArgumentNullException.ThrowIfNull(zoneId);

        if (!_registry.TryGet(channelId, zoneId, out string? controlTag) || controlTag is null)
        {
            Log.Error(_deviceId, $"ECP SetZoneEnable called with unknown pair ({channelId}, {zoneId}).");
            return;
        }

        _queue.TryEnqueue(EcpCommand.ControlSetString(controlTag, enable ? "true" : "false"));
        UpdateCacheAndRaise(channelId, zoneId, enable);
    }

    /// <summary>Toggles the zone-enable boolean.</summary>
    /// <param name="channelId">The owning input channel id.</param>
    /// <param name="zoneId">The zone id.</param>
    public void Toggle(string channelId, string zoneId)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        ArgumentNullException.ThrowIfNull(zoneId);

        bool current = _cache.TryGetValue((channelId, zoneId), out bool cached) && cached;
        Set(channelId, zoneId, !current);
    }

    /// <summary>Returns the cached zone-enable state.</summary>
    /// <param name="channelId">The owning input channel id.</param>
    /// <param name="zoneId">The zone id.</param>
    /// <returns>True if enabled.</returns>
    public bool Query(string channelId, string zoneId)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        ArgumentNullException.ThrowIfNull(zoneId);
        return _cache.TryGetValue((channelId, zoneId), out bool cached) && cached;
    }

    /// <summary>
    /// Reconciles the optimistic zone-enable cache against an
    /// inbound <c>cv</c> on the zone's control tag.
    /// </summary>
    /// <param name="channelId">The owning input channel id.</param>
    /// <param name="zoneId">The zone id.</param>
    /// <param name="enabled">The Core-reported value.</param>
    public void OnInboundZone(string channelId, string zoneId, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        ArgumentNullException.ThrowIfNull(zoneId);
        UpdateCacheAndRaise(channelId, zoneId, enabled);
    }

    private void UpdateCacheAndRaise(string channelId, string zoneId, bool next)
    {
        _cache.TryGetValue((channelId, zoneId), out bool prior);
        if (prior == next)
        {
            return;
        }

        _cache[(channelId, zoneId)] = next;
        ZoneEnableChanged?.Invoke(this, new GenericDualEventArgs<string, string>(channelId, zoneId));
    }
}
