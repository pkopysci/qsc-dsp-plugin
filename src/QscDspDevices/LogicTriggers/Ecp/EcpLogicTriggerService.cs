// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using QscDspDevices.Connectivity.Ecp;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol.Ecp;

namespace QscDspDevices.LogicTriggers.Ecp;

/// <summary>
/// ECP-side logic-trigger service. <c>Pulse</c> issues <c>ct CONTROL_ID</c>
/// against the registered named trigger control.
/// </summary>
internal sealed class EcpLogicTriggerService
{
    private readonly string _deviceId;
    private readonly LogicTriggerRegistry _registry;
    private readonly EcpCommandQueue _queue;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpLogicTriggerService"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="registry">The trigger registry.</param>
    /// <param name="queue">The ECP command queue.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public EcpLogicTriggerService(string deviceId, LogicTriggerRegistry registry, EcpCommandQueue queue)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(queue);

        _deviceId = deviceId;
        _registry = registry;
        _queue = queue;
    }

    /// <summary>Raised on observed trigger-state changes from the Core.</summary>
    public event EventHandler<gcu_common_utils.GenericEventArgs.GenericSingleEventArgs<string>>? LogicTriggerStateChanged;

    /// <summary>
    /// Reconciles an inbound <c>cv</c> on a registered trigger
    /// control. Emits <see cref="LogicTriggerStateChanged"/> when the
    /// observed state changes; trigger semantics are pulse-only on the
    /// framework surface, so the event fires on every observed
    /// transition.
    /// </summary>
    /// <param name="triggerId">The framework trigger id.</param>
    /// <param name="state">The Core-reported state.</param>
    public void OnInboundTrigger(string triggerId, bool state)
    {
        ArgumentNullException.ThrowIfNull(triggerId);
        _ = state;
        LogicTriggerStateChanged?.Invoke(this, new gcu_common_utils.GenericEventArgs.GenericSingleEventArgs<string>(triggerId));
    }

    /// <summary>Pulses the trigger via <c>ct CONTROL_ID</c>.</summary>
    /// <param name="id">The framework trigger id.</param>
    public void Pulse(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (!_registry.TryGet(id, out string? tag) || tag is null)
        {
            Log.Error(_deviceId, $"ECP Pulse called with unknown trigger id '{id}'.");
            return;
        }

        _queue.TryEnqueue(EcpCommand.ControlTrigger(tag));
    }
}
