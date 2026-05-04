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
