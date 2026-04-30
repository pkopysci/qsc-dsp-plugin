// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.PropertyTests.Protocol;

/// <summary>
/// FsCheck property-based tests for <see cref="CommandQueue"/>. The
/// FIFO invariant is the load-bearing contract; we exercise it under
/// random sequences of (enqueue, dequeue) operations.
/// </summary>
public class CommandQueueProperties
{
    /// <summary>
    /// Sequential enqueue+dequeue under random N preserves the order of
    /// the request ids.
    /// </summary>
    [Property]
    public bool Sequential_FIFO_preserves_id_order(PositiveInt countWrapper)
    {
        int count = Math.Min(countWrapper.Get, 1024);

        using var queue = new CommandQueue("dsp-prop", capacity: 1024);
        queue.StartAccepting();

        var enqueued = new List<long>();
        for (int i = 1; i <= count; i++)
        {
            var req = new JsonRpcRequest { Id = i, Method = "T" };
            if (queue.TryEnqueue(req))
            {
                enqueued.Add(req.Id);
            }
        }

        var dequeued = queue.SnapshotPending().Select(r => r.Id).ToArray();
        return dequeued.SequenceEqual(enqueued);
    }

    /// <summary>
    /// Drain after enqueue empties the queue regardless of how many were
    /// enqueued.
    /// </summary>
    [Property]
    public bool Drain_empties_the_queue(PositiveInt countWrapper)
    {
        int count = Math.Min(countWrapper.Get, 1024);

        using var queue = new CommandQueue("dsp-prop", capacity: 1024);
        queue.StartAccepting();

        for (int i = 1; i <= count; i++)
        {
            queue.TryEnqueue(new JsonRpcRequest { Id = i, Method = "T" });
        }

        queue.Drain();
        return queue.SnapshotPending().Count == 0 && !queue.IsAccepting;
    }

    /// <summary>
    /// While the queue is not accepting, every enqueue returns false
    /// regardless of input.
    /// </summary>
    [Property]
    public bool Enqueue_when_not_accepting_always_returns_false(PositiveInt countWrapper)
    {
        int count = Math.Min(countWrapper.Get, 100);

        using var queue = new CommandQueue("dsp-prop", capacity: 1024);

        // Note: NOT calling StartAccepting.
        for (int i = 1; i <= count; i++)
        {
            if (queue.TryEnqueue(new JsonRpcRequest { Id = i, Method = "T" }))
            {
                return false;
            }
        }

        return true;
    }
}
