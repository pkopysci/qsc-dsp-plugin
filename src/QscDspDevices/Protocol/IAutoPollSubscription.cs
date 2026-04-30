// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.Protocol;

/// <summary>
/// Receiver for QRC <c>ChangeGroup.AutoPoll</c> push messages. The QRC
/// protocol reuses the original AutoPoll request's id on every push, so
/// the dispatcher routes responses with that id to the registered
/// subscription instead of completing them as one-shot replies.
/// </summary>
/// <remarks>
/// Subscriptions are registered at the dispatcher via
/// <c>RegisterAutoPoll</c> and unregistered via
/// <c>UnregisterAutoPoll</c>. M2 only defines the routing surface; the
/// concrete consumers of these pushes are added by M3 (audio control)
/// and M4 (routing).
/// </remarks>
public interface IAutoPollSubscription
{
    /// <summary>
    /// Called by the dispatcher whenever a JSON-RPC response arrives whose
    /// id matches this subscription's id. The supplied response carries
    /// the latest changes.
    /// </summary>
    /// <param name="response">The pushed response from the Core.</param>
    void OnPush(JsonRpcResponse response);
}
