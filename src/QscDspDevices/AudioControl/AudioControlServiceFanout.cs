// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using QscDspDevices.Protocol.ChangeGroup;

namespace QscDspDevices.AudioControl;

/// <summary>
/// Three-way dispatcher for AutoPoll deltas: routerTag → routing
/// service, zone-tag → zone-enable service, otherwise → audio
/// control service. Allocation-free; the registries' reverse-lookup
/// predicates make each branch O(1).
/// </summary>
/// <remarks>
/// <para>
/// Priority order matters and is documented: a tag registered as both
/// a router-tag and a zone-tag (a Designer-side configuration error)
/// dispatches to the routing service. Audio-control runs last because
/// it owns the "unknown tag" fast path (already silent).
/// </para>
/// </remarks>
public sealed class AudioControlServiceFanout
{
    private readonly AudioChannelRegistry _channelRegistry;
    private readonly AudioZoneRegistry _zoneRegistry;
    private readonly AudioRoutingService _routing;
    private readonly AudioZoneEnableService _zone;
    private readonly AudioControlService _audio;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioControlServiceFanout"/> class.
    /// </summary>
    /// <param name="channelRegistry">The audio channel registry (router-tag predicate).</param>
    /// <param name="zoneRegistry">The zone registry (zone-tag predicate).</param>
    /// <param name="routing">The routing service.</param>
    /// <param name="zone">The zone-enable service.</param>
    /// <param name="audio">The audio-control service (level/mute fallback).</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public AudioControlServiceFanout(
        AudioChannelRegistry channelRegistry,
        AudioZoneRegistry zoneRegistry,
        AudioRoutingService routing,
        AudioZoneEnableService zone,
        AudioControlService audio)
    {
        ArgumentNullException.ThrowIfNull(channelRegistry);
        ArgumentNullException.ThrowIfNull(zoneRegistry);
        ArgumentNullException.ThrowIfNull(routing);
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(audio);

        _channelRegistry = channelRegistry;
        _zoneRegistry = zoneRegistry;
        _routing = routing;
        _zone = zone;
        _audio = audio;
    }

    /// <summary>
    /// Dispatches a single AutoPoll delta to the matching service.
    /// </summary>
    /// <param name="delta">The delta.</param>
    public void Dispatch(ChangeGroupDelta delta)
    {
        ArgumentNullException.ThrowIfNull(delta);

        if (_channelRegistry.IsRouterTag(delta.Name))
        {
            _routing.OnDeviceUpdate(delta);
            return;
        }

        if (_zoneRegistry.IsZoneTag(delta.Name))
        {
            _zone.OnDeviceUpdate(delta);
            return;
        }

        _audio.OnDeviceUpdate(delta);
    }
}
