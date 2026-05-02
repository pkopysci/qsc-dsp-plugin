// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using QscDspDevices.LogicTriggers;
using QscDspDevices.Protocol.ChangeGroup;

namespace QscDspDevices.AudioControl;

/// <summary>
/// Four-way dispatcher for AutoPoll deltas: routerTag → routing,
/// zoneTag → zone-enable, triggerTag → logic-trigger, otherwise →
/// audio control. Allocation-free; the registries' reverse-lookup
/// predicates make each branch O(1).
/// </summary>
/// <remarks>
/// <para>
/// Priority order is fixed: a tag claimed by multiple registries (a
/// Designer-side configuration error) dispatches to the highest-
/// priority match. Audio control runs last because it owns the
/// "unknown tag" fast path (already silent).
/// </para>
/// </remarks>
public sealed class AudioControlServiceFanout
{
    private readonly AudioChannelRegistry _channelRegistry;
    private readonly AudioZoneRegistry _zoneRegistry;
    private readonly LogicTriggerRegistry? _triggerRegistry;
    private readonly AudioRoutingService _routing;
    private readonly AudioZoneEnableService _zone;
    private readonly LogicTriggerService? _trigger;
    private readonly AudioControlService _audio;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioControlServiceFanout"/> class
    /// without logic-trigger support (M3 / M4-only configurations).
    /// </summary>
    /// <param name="channelRegistry">The audio channel registry.</param>
    /// <param name="zoneRegistry">The zone registry.</param>
    /// <param name="routing">The routing service.</param>
    /// <param name="zone">The zone-enable service.</param>
    /// <param name="audio">The audio-control service.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public AudioControlServiceFanout(
        AudioChannelRegistry channelRegistry,
        AudioZoneRegistry zoneRegistry,
        AudioRoutingService routing,
        AudioZoneEnableService zone,
        AudioControlService audio)
        : this(channelRegistry, zoneRegistry, triggerRegistry: null, routing, zone, trigger: null, audio)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioControlServiceFanout"/> class
    /// with M5 logic-trigger support.
    /// </summary>
    /// <param name="channelRegistry">The audio channel registry (router-tag predicate).</param>
    /// <param name="zoneRegistry">The zone registry (zone-tag predicate).</param>
    /// <param name="triggerRegistry">The logic-trigger registry (trigger-tag predicate); null when M5 is not wired.</param>
    /// <param name="routing">The routing service.</param>
    /// <param name="zone">The zone-enable service.</param>
    /// <param name="trigger">The logic-trigger service; null when M5 is not wired.</param>
    /// <param name="audio">The audio-control service (level/mute fallback).</param>
    /// <exception cref="ArgumentNullException">If any required argument is null.</exception>
    public AudioControlServiceFanout(
        AudioChannelRegistry channelRegistry,
        AudioZoneRegistry zoneRegistry,
        LogicTriggerRegistry? triggerRegistry,
        AudioRoutingService routing,
        AudioZoneEnableService zone,
        LogicTriggerService? trigger,
        AudioControlService audio)
    {
        ArgumentNullException.ThrowIfNull(channelRegistry);
        ArgumentNullException.ThrowIfNull(zoneRegistry);
        ArgumentNullException.ThrowIfNull(routing);
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(audio);

        _channelRegistry = channelRegistry;
        _zoneRegistry = zoneRegistry;
        _triggerRegistry = triggerRegistry;
        _routing = routing;
        _zone = zone;
        _trigger = trigger;
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

        if (_trigger is not null && _triggerRegistry is not null && _triggerRegistry.IsTriggerTag(delta.Name))
        {
            _trigger.OnDeviceUpdate(delta);
            return;
        }

        _audio.OnDeviceUpdate(delta);
    }
}
