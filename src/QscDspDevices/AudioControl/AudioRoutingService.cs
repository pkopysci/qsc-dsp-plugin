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
/// Orchestrates the framework-side <c>IAudioRoutable</c> surface against
/// the QRC wire: sends <c>Control.Set</c> on each output's registered
/// <c>routerTag</c>, holds the per-output <c>currentSourceId</c> cache,
/// and raises <c>AudioRouteChanged</c> when the cache transitions.
/// </summary>
/// <remarks>
/// <para>
/// Sibling of <see cref="AudioControlService"/>; the cache semantics
/// are identical — see that type's "intent semantics" remarks. The
/// bank-index ↔ channel-id translation is done via
/// <see cref="AudioChannelRegistry.TryGetInputIdByRouterIndex"/>.
/// </para>
/// <para>
/// <b>Cache semantics — intent, not state.</b> <see cref="Route"/> and
/// <see cref="Clear"/> update the cache <i>before</i> attempting
/// <c>TryEnqueue</c>. While disconnected, the queue silently refuses
/// the wire write; the cache still reflects the framework's most
/// recent intent. On reconnect the AutoPoll on the routerTag replays
/// the Core's real value, and the cache reconciles (raising
/// <see cref="RouteChanged"/> if the Core's reality disagrees with
/// the cached intent). Framework-side reads via
/// <see cref="GetCurrentSource"/> during the disconnected window
/// therefore reflect intent, not server state.
/// </para>
/// </remarks>
public sealed class AudioRoutingService
{
    /// <summary>The QSC sentinel value for "no source selected".</summary>
    public const int ClearedSourceValue = 0;

    private readonly string _deviceId;
    private readonly AudioChannelRegistry _registry;
    private readonly CommandQueue _queue;
    private readonly IdGenerator _ids;

    private readonly ConcurrentDictionary<string, string> _outputToSource = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioRoutingService"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="registry">The channel registry.</param>
    /// <param name="queue">The command queue requests are enqueued on.</param>
    /// <param name="ids">The shared monotonic id generator.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public AudioRoutingService(string deviceId, AudioChannelRegistry registry, CommandQueue queue, IdGenerator ids)
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

    /// <summary>Raised when an output's cached source id transitions.</summary>
    public event EventHandler<GenericDualEventArgs<string, string>>? RouteChanged;

    /// <summary>
    /// Implements <c>IAudioRoutable.RouteAudio</c>. Resolves the
    /// source's bank index, enqueues <c>Control.Set { Name=routerTag,
    /// Value=bankIndex }</c>, and updates the cache optimistically.
    /// Unknown source or output id logs <c>Logger.Error</c> and is a
    /// silent no-op.
    /// </summary>
    /// <param name="sourceId">The framework input channel id.</param>
    /// <param name="outputId">The framework output channel id.</param>
    public void Route(string sourceId, string outputId)
    {
        ArgumentNullException.ThrowIfNull(sourceId);
        ArgumentNullException.ThrowIfNull(outputId);

        if (!_registry.TryGetChannel(outputId, out AudioChannel? output) || output is null || output.IsInput)
        {
            Log.Error(_deviceId, $"RouteAudio called with unknown output id '{outputId}'.");
            return;
        }

        if (string.IsNullOrEmpty(output.RouterTag))
        {
            Log.Error(_deviceId, $"RouteAudio('{sourceId}', '{outputId}') — output has no routerTag registered; cannot route.");
            return;
        }

        if (!_registry.TryGetChannel(sourceId, out AudioChannel? source) || source is null || !source.IsInput)
        {
            Log.Error(_deviceId, $"RouteAudio called with unknown source id '{sourceId}'.");
            return;
        }

        // RouterIndex 0 is the QSC "no source" sentinel (see Clear). A
        // source registered with routerIndex <= 0 cannot be routed
        // because it would clear the output instead. Reject explicitly
        // so misconfigured inputs surface as Logger.Error rather than
        // as a silent route-then-clear that disagrees with the cache.
        // Routing uses the input's RouterIndex per QSC's design;
        // BankIndex is reserved for snapshot/preset addressing.
        if (source.RouterIndex < 1)
        {
            Log.Error(
                _deviceId,
                $"RouteAudio('{sourceId}', '{outputId}') — source routerIndex {source.RouterIndex} is invalid (must be >= 1; routerIndex 0 is the QSC 'cleared' sentinel).");
            return;
        }

        var request = new JsonRpcRequest
        {
            Id = _ids.Next(),
            Method = "Control.Set",
            Params = new { Name = output.RouterTag, Value = source.RouterIndex },
        };

        UpdateCacheAndRaise(outputId, sourceId);
        _queue.TryEnqueue(request);
    }

    /// <summary>
    /// Implements <c>IAudioRoutable.ClearAudioRoute</c>. Sends
    /// <c>Control.Set { Value = 0 }</c> and updates the cache to
    /// the empty string.
    /// </summary>
    /// <param name="outputId">The framework output channel id.</param>
    public void Clear(string outputId)
    {
        ArgumentNullException.ThrowIfNull(outputId);

        if (!_registry.TryGetChannel(outputId, out AudioChannel? output) || output is null || output.IsInput)
        {
            Log.Error(_deviceId, $"ClearAudioRoute called with unknown output id '{outputId}'.");
            return;
        }

        if (string.IsNullOrEmpty(output.RouterTag))
        {
            Log.Error(_deviceId, $"ClearAudioRoute('{outputId}') — output has no routerTag registered.");
            return;
        }

        var request = new JsonRpcRequest
        {
            Id = _ids.Next(),
            Method = "Control.Set",
            Params = new { Name = output.RouterTag, Value = ClearedSourceValue },
        };

        UpdateCacheAndRaise(outputId, string.Empty);
        _queue.TryEnqueue(request);
    }

    /// <summary>
    /// Implements <c>IAudioRoutable.GetCurrentAudioSource</c>. Returns
    /// the cached source channel id, or empty string for unknown
    /// outputs / unpopulated cache / cleared route.
    /// </summary>
    /// <param name="outputId">The framework output channel id.</param>
    /// <returns>The cached source channel id, or empty string.</returns>
    public string GetCurrentSource(string outputId)
    {
        ArgumentNullException.ThrowIfNull(outputId);
        return _outputToSource.TryGetValue(outputId, out string? source) ? source : string.Empty;
    }

    /// <summary>
    /// AutoPoll delta callback. The fan-out dispatcher routes router-
    /// tag deltas here; other tags should be filtered upstream.
    /// </summary>
    /// <param name="delta">The parsed delta.</param>
    public void OnDeviceUpdate(ChangeGroupDelta delta)
    {
        ArgumentNullException.ThrowIfNull(delta);

        // Find the output that owns this router tag.
        if (!_registry.TryGetChannelIdByTag(delta.Name, out string? outputId)
            || outputId is null
            || !_registry.TryGetChannel(outputId, out AudioChannel? output)
            || output is null
            || string.IsNullOrEmpty(output.RouterTag)
            || !string.Equals(output.RouterTag, delta.Name, StringComparison.Ordinal))
        {
            return;
        }

        int routerIndex = ExtractInteger(delta);
        string newSourceId = routerIndex == ClearedSourceValue
            ? string.Empty
            : _registry.TryGetInputIdByRouterIndex(routerIndex, out string? sourceId) && sourceId is not null
                ? sourceId
                : string.Empty;

        UpdateCacheAndRaise(outputId, newSourceId);
    }

    private static int ExtractInteger(ChangeGroupDelta delta)
    {
        return delta.Value.Type switch
        {
            JTokenType.Integer => delta.Value.ToObject<int>(),
            JTokenType.Float => (int)Math.Round(delta.Value.ToObject<double>()),
            _ => ClearedSourceValue,
        };
    }

    private void UpdateCacheAndRaise(string outputId, string newSourceId)
    {
        bool hadPrior = _outputToSource.TryGetValue(outputId, out string? prior);

        // First-write of an empty source for a never-cached output is
        // a no-op transition: the cache implicitly returns "" for unknown
        // outputs (see GetCurrentSource), so going from "implicit empty"
        // to "explicit empty" is not an observable change and must NOT
        // raise the event. This matters most for Clear() against an
        // output that was never routed since Connect.
        if (!hadPrior && string.IsNullOrEmpty(newSourceId))
        {
            return;
        }

        bool changed = !hadPrior || !string.Equals(prior, newSourceId, StringComparison.Ordinal);
        _outputToSource[outputId] = newSourceId;

        if (!changed)
        {
            return;
        }

        RouteChanged?.Invoke(this, new GenericDualEventArgs<string, string>(_deviceId, outputId));
    }
}
