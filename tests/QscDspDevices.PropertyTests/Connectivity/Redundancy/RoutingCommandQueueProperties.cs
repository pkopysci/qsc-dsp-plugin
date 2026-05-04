// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FsCheck.Xunit;
using QscDspDevices.Connectivity.Redundancy;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.PropertyTests.Connectivity.Redundancy;

/// <summary>
/// FsCheck property tests for <see cref="RoutingCommandQueue"/>. Pins
/// the facade-equivalence invariant: when an inner queue is set,
/// every <c>TryEnqueue</c> on the facade must produce the same
/// observable result and the same enqueued payload as a direct call
/// to the inner queue.
/// </summary>
public class RoutingCommandQueueProperties
{
    /// <summary>
    /// SetActive then TryEnqueue is observably equivalent to direct
    /// inner.TryEnqueue. Guards against the facade silently dropping,
    /// reordering, or duplicating requests.
    /// </summary>
    /// <param name="ids">A non-empty random list of request ids.</param>
    /// <returns>True if every request landed on the inner queue in order.</returns>
    [Property]
    public bool SetActive_then_TryEnqueue_lands_each_request_on_inner(long[] ids)
    {
        // Property handles the empty-array case trivially.
        if (ids is null || ids.Length == 0)
        {
            return true;
        }

        const string deviceId = "dsp-1";
        using var inner = new CommandQueue(deviceId);
        inner.StartAccepting();
        using var facade = new RoutingCommandQueue(deviceId);
        facade.SetActive(inner);

        foreach (long id in ids)
        {
            bool ok = facade.TryEnqueue(new JsonRpcRequest { Id = id, Method = "X" });
            if (!ok)
            {
                return false;
            }
        }

        IReadOnlyList<JsonRpcRequest> drained = inner.SnapshotPending();
        if (drained.Count != ids.Length)
        {
            return false;
        }

        for (int i = 0; i < ids.Length; i++)
        {
            if (drained[i].Id != ids[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// SetActive(null) followed by TryEnqueue always returns false,
    /// regardless of how many requests had previously been routed.
    /// </summary>
    /// <param name="seed">Non-negative count of pre-null successful enqueues.</param>
    /// <returns>True if the post-null TryEnqueue is refused.</returns>
    [Property]
    public bool SetActive_null_refuses_subsequent_TryEnqueue(byte seed)
    {
        const string deviceId = "dsp-1";
        using var inner = new CommandQueue(deviceId);
        inner.StartAccepting();
        using var facade = new RoutingCommandQueue(deviceId);
        facade.SetActive(inner);

        for (int i = 0; i < seed; i++)
        {
            facade.TryEnqueue(new JsonRpcRequest { Id = i, Method = "X" });
        }

        facade.SetActive(null);
        bool result = facade.TryEnqueue(new JsonRpcRequest { Id = 9999, Method = "Y" });
        return result == false;
    }
}
