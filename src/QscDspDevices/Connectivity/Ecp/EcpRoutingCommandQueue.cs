// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using QscDspDevices.Plugin;

namespace QscDspDevices.Connectivity.Ecp;

/// <summary>
/// ECP-side equivalent of <c>RoutingCommandQueue</c>: a thin facade
/// over <see cref="EcpCommandQueue"/> that the redundant-pair
/// coordinator re-points on every active-slot transition. The
/// service tier holds a reference to this facade and enqueues without
/// knowing which Core is currently active.
/// </summary>
internal sealed class EcpRoutingCommandQueue : EcpCommandQueue
{
    private readonly object _routeLock = new();
    private readonly string _facadeDeviceId;
    private EcpCommandQueue? _activeQueue;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpRoutingCommandQueue"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    public EcpRoutingCommandQueue(string deviceId)
        : base(deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        _facadeDeviceId = deviceId;
    }

    /// <summary>Sets (or clears) the underlying queue every TryEnqueue forwards to.</summary>
    /// <param name="queue">The new active queue, or null.</param>
    public void SetActive(EcpCommandQueue? queue)
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
            ? "ECP routing queue: no active Core; subsequent writes will be refused."
            : "ECP routing queue: active Core set; writes will be forwarded.";
        Log.Notice(_facadeDeviceId, message);
    }

    /// <inheritdoc />
    public override bool TryEnqueue(string command)
    {
        ArgumentNullException.ThrowIfNull(command);

        EcpCommandQueue? snapshot;
        lock (_routeLock)
        {
            snapshot = _activeQueue;
        }

        if (snapshot is null)
        {
            Log.Error(_facadeDeviceId, "ECP routing queue: no active Core; refusing send.");
            return false;
        }

        return snapshot.TryEnqueue(command);
    }
}
