// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Concurrent;
using gcu_common_utils.GenericEventArgs;
using QscDspDevices.Connectivity.Ecp;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol.Ecp;

namespace QscDspDevices.AudioControl.Ecp;

/// <summary>
/// ECP-side audio-routing service. Each registered output's
/// <see cref="AudioChannel.RouterTag"/> is a named matrix-crosspoint
/// control on the Q-SYS Core, addressable via <c>csv</c>. ECP cannot
/// address crosspoints by integer index, but the framework's
/// <see cref="AudioChannelRegistry"/> already requires a named
/// router-tag for every routable output, so the QRC and ECP paths
/// behave identically on the use cases the framework exposes.
/// </summary>
internal sealed class EcpAudioRoutingService
{
    /// <summary>The QSC "no source" sentinel value written on Clear.</summary>
    public const int ClearedSourceValue = 0;

    private readonly string _deviceId;
    private readonly AudioChannelRegistry _registry;
    private readonly EcpCommandQueue _queue;
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpAudioRoutingService"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="registry">The audio-channel registry.</param>
    /// <param name="queue">The ECP command queue.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public EcpAudioRoutingService(string deviceId, AudioChannelRegistry registry, EcpCommandQueue queue)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(queue);

        _deviceId = deviceId;
        _registry = registry;
        _queue = queue;
    }

    /// <summary>Raised on each route change with (outputId, sourceId).</summary>
    public event EventHandler<GenericDualEventArgs<string, string>>? RouteChanged;

    /// <summary>Routes a source to an output via <c>csv</c> on the output's router tag.</summary>
    /// <param name="sourceId">The framework source id.</param>
    /// <param name="outputId">The framework output id.</param>
    public void Route(string sourceId, string outputId)
    {
        ArgumentNullException.ThrowIfNull(sourceId);
        ArgumentNullException.ThrowIfNull(outputId);

        if (!_registry.TryGetChannel(outputId, out AudioChannel? output) || output is null || output.IsInput)
        {
            Log.Error(_deviceId, $"ECP Route called with unknown output id '{outputId}'.");
            return;
        }

        if (string.IsNullOrEmpty(output.RouterTag))
        {
            Log.Error(_deviceId, $"ECP Route('{sourceId}', '{outputId}') — output has no routerTag.");
            return;
        }

        if (!_registry.TryGetChannel(sourceId, out AudioChannel? source) || source is null || !source.IsInput)
        {
            Log.Error(_deviceId, $"ECP Route called with unknown source id '{sourceId}'.");
            return;
        }

        if (source.RouterIndex < 1)
        {
            Log.Error(_deviceId, $"ECP Route('{sourceId}', '{outputId}') — source routerIndex {source.RouterIndex} invalid (must be ≥ 1).");
            return;
        }

        _queue.TryEnqueue(EcpCommand.ControlSetValue(output.RouterTag, source.RouterIndex));
        UpdateCacheAndRaise(outputId, sourceId);
    }

    /// <summary>Clears the output by writing 0 to its router tag.</summary>
    /// <param name="outputId">The framework output id.</param>
    public void Clear(string outputId)
    {
        ArgumentNullException.ThrowIfNull(outputId);
        if (!_registry.TryGetChannel(outputId, out AudioChannel? output) || output is null || output.IsInput)
        {
            Log.Error(_deviceId, $"ECP Clear called with unknown output id '{outputId}'.");
            return;
        }

        if (string.IsNullOrEmpty(output.RouterTag))
        {
            Log.Error(_deviceId, $"ECP Clear('{outputId}') — output has no routerTag.");
            return;
        }

        _queue.TryEnqueue(EcpCommand.ControlSetValue(output.RouterTag, ClearedSourceValue));
        UpdateCacheAndRaise(outputId, string.Empty);
    }

    /// <summary>Returns the cached source id for an output (empty if none).</summary>
    /// <param name="outputId">The framework output id.</param>
    /// <returns>The cached source id.</returns>
    public string Query(string outputId)
    {
        ArgumentNullException.ThrowIfNull(outputId);
        return _cache.TryGetValue(outputId, out string? sourceId) ? sourceId : string.Empty;
    }

    /// <summary>
    /// Reconciles the optimistic route cache against an inbound
    /// <c>cv</c> on a router tag. The Core's value is the source's
    /// router index; we look up the framework source id for it. A
    /// value of 0 means "cleared".
    /// </summary>
    /// <param name="output">The registered output channel whose routerTag matched the inbound.</param>
    /// <param name="routerIndex">The router index reported by the Core.</param>
    public void OnInboundRoute(AudioChannel output, int routerIndex)
    {
        ArgumentNullException.ThrowIfNull(output);

        string sourceId = string.Empty;
        if (routerIndex > 0 && _registry.TryGetInputIdByRouterIndex(routerIndex, out string? id) && id is not null)
        {
            sourceId = id;
        }

        UpdateCacheAndRaise(output.Id, sourceId);
    }

    private void UpdateCacheAndRaise(string outputId, string sourceId)
    {
        _cache.TryGetValue(outputId, out string? prior);
        if (string.Equals(prior, sourceId, StringComparison.Ordinal))
        {
            return;
        }

        _cache[outputId] = sourceId;
        RouteChanged?.Invoke(this, new GenericDualEventArgs<string, string>(outputId, sourceId));
    }
}
