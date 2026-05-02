// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using gcu_common_utils.GenericEventArgs;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.LogicTriggers;

/// <summary>
/// Orchestrates the framework-side <c>IDspLogicTriggerSupport</c>
/// surface. <see cref="Pulse"/> sends <c>Control.Set { Value=true }</c>
/// against the registered tag (no follow-up <c>false</c> — QSC
/// triggers auto-reset on the design side). AutoPoll deltas raise
/// <see cref="LogicTriggerStateChanged"/> with the trigger id.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cache-less by design.</b> The framework's
/// <c>DspLogicTriggerStateChanged</c> event signals "this trigger
/// transitioned" (Single-arg, just the id), not "the new value
/// is X". Coalescing on a cached value would suppress legitimate
/// consecutive pulses on a momentary trigger that holds <c>true</c>
/// briefly. The service therefore raises the event on every
/// AutoPoll delta whose tag is registered.
/// </para>
/// <para>
/// <b>Reconnect re-fires the event.</b> A consequence of cache-less
/// dispatch: the first AutoPoll after every reconnect re-fires
/// <c>LogicTriggerStateChanged</c> for every registered trigger,
/// even when the Core's pre-disconnect state matched the post-
/// reconnect state. Downstream consumers that need transition-only
/// semantics should cache at their own layer.
/// </para>
/// <para>
/// <b>Pulse failure is invisible to the framework.</b> Fire-and-
/// forget — the dispatcher logs an error response (e.g. <c>-32604</c>
/// Standby), but no signal reaches the framework because
/// <c>IDspLogicTriggerSupport.PulseDspLogicTrigger</c> is
/// void-returning and exposes no failure channel. Same shape as
/// <c>AudioControlService</c>'s mute/level Set path.
/// </para>
/// </remarks>
public sealed class LogicTriggerService
{
    private readonly string _deviceId;
    private readonly LogicTriggerRegistry _registry;
    private readonly CommandQueue _queue;
    private readonly IdGenerator _ids;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogicTriggerService"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="registry">The trigger registry.</param>
    /// <param name="queue">The command queue requests are enqueued on.</param>
    /// <param name="ids">The shared monotonic id generator.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public LogicTriggerService(string deviceId, LogicTriggerRegistry registry, CommandQueue queue, IdGenerator ids)
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

    /// <summary>
    /// Raised on every AutoPoll delta whose tag is registered as a
    /// logic trigger. The arg is the trigger id; per
    /// <c>framework-docs/gcu-hardware-service/IDspLogicTriggerSupport.md</c>
    /// this is a Single-arg event.
    /// </summary>
    public event EventHandler<GenericSingleEventArgs<string>>? LogicTriggerStateChanged;

    /// <summary>
    /// Implements <c>IDspLogicTriggerSupport.PulseDspLogicTrigger</c>.
    /// Looks up the registered tag and enqueues <c>Control.Set { Value=true }</c>.
    /// Unknown id logs <c>Logger.Error</c> and is a silent no-op.
    /// </summary>
    /// <param name="id">The framework trigger id.</param>
    public void Pulse(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (!_registry.TryGet(id, out string? tagName) || tagName is null)
        {
            Log.Error(_deviceId, $"PulseDspLogicTrigger called with unknown trigger id '{id}'.");
            return;
        }

        var request = new JsonRpcRequest
        {
            Id = _ids.Next(),
            Method = "Control.Set",
            Params = new { Name = tagName, Value = true },
        };

        _queue.TryEnqueue(request);
    }

    /// <summary>
    /// AutoPoll delta callback. The fan-out dispatcher routes
    /// trigger-tag deltas here. Raises the framework event on every
    /// delta (no cache; see class remarks).
    /// </summary>
    /// <param name="delta">The parsed delta.</param>
    public void OnDeviceUpdate(ChangeGroupDelta delta)
    {
        ArgumentNullException.ThrowIfNull(delta);

        if (!_registry.TryGetIdByTag(delta.Name, out string? id) || id is null)
        {
            // Should not happen — the fanout filtered upstream — but
            // belt-and-braces makes the service safe to call directly
            // from a test that bypasses the fanout.
            return;
        }

        LogicTriggerStateChanged?.Invoke(this, new GenericSingleEventArgs<string>(id));
    }
}
