// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using QscDspDevices.Plugin;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.Connectivity.Redundancy;

/// <summary>
/// Command-queue facade that extends <see cref="CommandQueue"/> for
/// type-compatibility with the M3-M5 service tier. Overrides
/// <see cref="CommandQueue.TryEnqueue"/> to forward every enqueue to
/// whichever underlying queue the redundant pair coordinator
/// currently designates as active (via <see cref="SetActive"/>).
/// </summary>
/// <remarks>
/// <para>
/// The M3-M5 services hold a <see cref="CommandQueue"/> reference and
/// call <c>TryEnqueue</c>. M6 swaps that reference for this facade
/// so the same call sites work unchanged across the pair. The base
/// class's internal channel and lifecycle methods (<c>StartAccepting</c>,
/// <c>Drain</c>, <c>DequeueAsync</c>) are NOT used through the facade
/// — the facade's job is producer-side routing only. Each underlying
/// <see cref="ConnectionManager"/> owns its own real queue and runs
/// its own send loop against that queue.
/// </para>
/// <para>
/// When no slot is active (initial pre-EngineStatus window, or both
/// Standby / disconnected), <c>TryEnqueue</c> returns <c>false</c>
/// and logs <c>Logger.Error</c> — matches the M2 "queue refuses
/// while disconnected" contract.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Deliberate facade over CommandQueue — sharing the 'Queue' suffix makes the M3-M5 service-tier swap-in obvious.")]
public sealed class RoutingCommandQueue : CommandQueue
{
    private readonly string _facadeDeviceId;
    private readonly object _routeLock = new();
    private CommandQueue? _activeQueue;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoutingCommandQueue"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id (for log messages).</param>
    /// <exception cref="ArgumentNullException">If <paramref name="deviceId"/> is null.</exception>
    public RoutingCommandQueue(string deviceId)
        : base(deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        _facadeDeviceId = deviceId;
    }

    /// <summary>
    /// Sets (or clears) the underlying queue every <c>TryEnqueue</c>
    /// is forwarded to. Pass <c>null</c> to mark "no active Core".
    /// Logs <c>Logger.Notice</c> on every transition.
    /// </summary>
    /// <param name="queue">The new active queue, or <c>null</c>.</param>
    public void SetActive(CommandQueue? queue)
    {
        lock (_routeLock)
        {
            if (ReferenceEquals(_activeQueue, queue))
            {
                return;
            }

            _activeQueue = queue;
        }

        string message = queue is null
            ? "Routing command queue: no active Core; subsequent writes will be refused."
            : "Routing command queue: active Core set; writes will be forwarded.";
        Log.Notice(_facadeDeviceId, message);
    }

    /// <inheritdoc />
    public override bool TryEnqueue(JsonRpcRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        CommandQueue? snapshot;
        lock (_routeLock)
        {
            snapshot = _activeQueue;
        }

        if (snapshot is null)
        {
            Log.Error(
                _facadeDeviceId,
                $"Routing command queue: no active Core; refusing send (method={request.Method}, id={request.Id}).");
            return false;
        }

        return snapshot.TryEnqueue(request);
    }
}
