// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using gcu_common_utils.GenericEventArgs;
using QscDspDevices.AudioControl;
using QscDspDevices.AudioControl.Ecp;
using QscDspDevices.LogicTriggers;
using QscDspDevices.LogicTriggers.Ecp;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol.Ecp;

namespace QscDspDevices.Connectivity.Ecp;

/// <summary>
/// Subscribes to <see cref="EcpDispatcher.ResponseReceived"/>, filters
/// <see cref="EcpResponseKind.ControlValue"/> lines, and routes each
/// to the right ECP service-tier callback (audio control, routing,
/// zone-enable, logic trigger). Mirrors the QRC
/// <c>AudioControlServiceFanout</c> shape but works off raw ECP
/// <c>cv</c> tuples instead of QRC change-group deltas.
/// </summary>
internal sealed class EcpAutoPollSubscription : IDisposable
{
    private readonly string _deviceId;
    private readonly EcpDispatcher _dispatcher;
    private readonly AudioChannelRegistry _channels;
    private readonly AudioZoneRegistry _zones;
    private readonly LogicTriggerRegistry _triggers;
    private readonly EcpAudioControlService _audio;
    private readonly EcpAudioRoutingService _routing;
    private readonly EcpAudioZoneEnableService _zoneEnable;
    private readonly EcpLogicTriggerService _logic;
    private readonly EventHandler<GenericSingleEventArgs<EcpResponse>> _handler;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpAutoPollSubscription"/> class
    /// and subscribes to the supplied dispatcher.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="dispatcher">The ECP dispatcher to listen on.</param>
    /// <param name="channels">The audio-channel registry.</param>
    /// <param name="zones">The audio-zone registry.</param>
    /// <param name="triggers">The logic-trigger registry.</param>
    /// <param name="audio">The ECP audio-control service.</param>
    /// <param name="routing">The ECP audio-routing service.</param>
    /// <param name="zoneEnable">The ECP zone-enable service.</param>
    /// <param name="logic">The ECP logic-trigger service.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public EcpAutoPollSubscription(
        string deviceId,
        EcpDispatcher dispatcher,
        AudioChannelRegistry channels,
        AudioZoneRegistry zones,
        LogicTriggerRegistry triggers,
        EcpAudioControlService audio,
        EcpAudioRoutingService routing,
        EcpAudioZoneEnableService zoneEnable,
        EcpLogicTriggerService logic)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(channels);
        ArgumentNullException.ThrowIfNull(zones);
        ArgumentNullException.ThrowIfNull(triggers);
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(routing);
        ArgumentNullException.ThrowIfNull(zoneEnable);
        ArgumentNullException.ThrowIfNull(logic);

        _deviceId = deviceId;
        _dispatcher = dispatcher;
        _channels = channels;
        _zones = zones;
        _triggers = triggers;
        _audio = audio;
        _routing = routing;
        _zoneEnable = zoneEnable;
        _logic = logic;

        _handler = OnResponse;
        _dispatcher.ResponseReceived += _handler;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _dispatcher.ResponseReceived -= _handler;
    }

    private void OnResponse(object? sender, GenericSingleEventArgs<EcpResponse> args)
    {
        if (args.Arg.Kind != EcpResponseKind.ControlValue)
        {
            return;
        }

        string? controlTag = args.Arg.ControlId;
        if (string.IsNullOrEmpty(controlTag))
        {
            return;
        }

        // Try each registry in turn. A given tag belongs to exactly
        // one — channel-level, channel-mute, channel-router, zone, or
        // trigger — so the first match wins. Unknown tags fall through
        // and are logged at Warn (the dispatcher already logged once
        // when the tag arrived; we add the "unmatched" detail here).
        if (TryRouteAudio(controlTag, args.Arg))
        {
            return;
        }

        if (TryRouteZone(controlTag, args.Arg))
        {
            return;
        }

        if (TryRouteTrigger(controlTag, args.Arg))
        {
            return;
        }

        Log.Warn(_deviceId, $"ECP cv update for unknown tag '{controlTag}'; ignoring.");
    }

    private bool TryRouteAudio(string tag, EcpResponse cv)
    {
        if (!_channels.TryGetChannelIdByTag(tag, out string? channelId) || channelId is null)
        {
            return false;
        }

        if (!_channels.TryGetChannel(channelId, out AudioChannel? channel) || channel is null)
        {
            return false;
        }

        if (string.Equals(channel.LevelTag, tag, StringComparison.Ordinal))
        {
            _audio.OnInboundLevel(channel, cv.Value);
            return true;
        }

        if (string.Equals(channel.MuteTag, tag, StringComparison.Ordinal))
        {
            // Display-string is "true" / "false" or the integer 1/0
            // form; treat any non-zero value as muted.
            _audio.OnInboundMute(channel, ParseBool(cv));
            return true;
        }

        if (!string.IsNullOrEmpty(channel.RouterTag) && string.Equals(channel.RouterTag, tag, StringComparison.Ordinal))
        {
            _routing.OnInboundRoute(channel, (int)cv.Value);
            return true;
        }

        return false;
    }

    private bool TryRouteZone(string tag, EcpResponse cv)
    {
        if (!_zones.TryGetPair(tag, out (string ChannelId, string ZoneId) pair))
        {
            return false;
        }

        _zoneEnable.OnInboundZone(pair.ChannelId, pair.ZoneId, ParseBool(cv));
        return true;
    }

    private bool TryRouteTrigger(string tag, EcpResponse cv)
    {
        if (!_triggers.TryGetIdByTag(tag, out string? triggerId) || triggerId is null)
        {
            return false;
        }

        _logic.OnInboundTrigger(triggerId, ParseBool(cv));
        return true;
    }

#pragma warning disable SA1204
    private static bool ParseBool(EcpResponse cv)
    {
        // ECP renders booleans as "true" / "false" in the display
        // string; the value is 1.0 or 0.0. Either is sufficient.
        if (string.Equals(cv.Display, "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(cv.Display, "false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return cv.Value != 0;
    }
#pragma warning restore SA1204
}
