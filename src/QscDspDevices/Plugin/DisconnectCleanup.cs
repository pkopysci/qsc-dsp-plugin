// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.Protocol.JsonRpc;
using QscDspDevices.Transport;

namespace QscDspDevices.Plugin;

/// <summary>
/// Helpers for the graceful-disconnect path. Currently provides a
/// best-effort <c>ChangeGroup.Destroy</c> enqueue used by both the
/// single-Core <see cref="QscDspTcp"/> and the redundant pair
/// coordinator. Kept separate so the access-modifier ordering rules
/// don't force the helper into either of those classes' private
/// regions.
/// </summary>
internal static class DisconnectCleanup
{
    /// <summary>
    /// Best-effort enqueue of a <c>ChangeGroup.Destroy</c> for the
    /// owning side's plugin change group. Idempotent and silent when
    /// any precondition is missing; logs <c>Logger.Warn</c> only on a
    /// rejected enqueue or a thrown exception (the Core's socket-close
    /// GC reclaims the group regardless).
    /// </summary>
    /// <param name="deviceId">Owning device id, used for log routing.</param>
    /// <param name="groupManager">The change-group manager for the side being torn down.</param>
    /// <param name="queue">The command queue for the side being torn down.</param>
    /// <param name="transport">Optional transport for an extra <see cref="IConnectionTransport.IsConnected"/> guard. Pass <c>null</c> to rely on the queue's own non-accepting guard.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "ChangeGroup.Destroy on disconnect is courtesy: the Core GCs the group on socket close. Any failure here must not block the disconnect or crash the host (README §\"Exception Handling\").")]
    internal static void TryEnqueueDestroy(string deviceId, ChangeGroupManager? groupManager, CommandQueue? queue, IConnectionTransport? transport)
    {
        if (groupManager is null || queue is null)
        {
            return;
        }

        if (transport is not null && !transport.IsConnected)
        {
            return;
        }

        try
        {
            JsonRpcRequest? destroy = groupManager.BuildDestroy(ChangeGroupManager.PluginGroupId);
            if (destroy is null)
            {
                return;
            }

            if (!queue.TryEnqueue(destroy))
            {
                Log.Warn(deviceId, "ChangeGroup.Destroy enqueue refused on disconnect; the Core's socket-close GC will reclaim the group.");
            }
        }
        catch (Exception ex)
        {
            Log.Warn(deviceId, $"ChangeGroup.Destroy attempt threw {ex.GetType().Name}: {ex.Message}; continuing disconnect.");
        }
    }
}
