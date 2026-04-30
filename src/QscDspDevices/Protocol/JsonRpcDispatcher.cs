// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using gcu_common_utils.GenericEventArgs;
using Newtonsoft.Json;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.Protocol;

/// <summary>
/// Dispatches inbound JSON-RPC frames received from the Q-SYS Core to
/// either:
/// <list type="bullet">
///   <item>An outstanding request awaiting its response (id correlation).</item>
///   <item>A registered <see cref="IAutoPollSubscription"/> whose id matches the inbound (AutoPoll push).</item>
///   <item>The <see cref="NotificationReceived"/> event (server-originated notification with no id).</item>
/// </list>
/// Unknown ids are logged at <c>Logger.Warn</c> and discarded.
/// </summary>
/// <remarks>
/// The dispatcher uses lock-free <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// instances for both maps so the receive-loop can publish without
/// contending with the send-loop's registrations.
/// </remarks>
public sealed class JsonRpcDispatcher
{
    private readonly string _deviceId;
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonRpcResponse>> _pending = new();
    private readonly ConcurrentDictionary<long, IAutoPollSubscription> _autoPolls = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRpcDispatcher"/> class
    /// tagged with the supplied device id (used for log messages).
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="deviceId"/> is null.</exception>
    public JsonRpcDispatcher(string deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        _deviceId = deviceId;
    }

    /// <summary>
    /// Raised for server-originated notifications (frames with a
    /// <c>method</c> and no <c>id</c>, e.g. <c>EngineStatus</c>).
    /// </summary>
    public event EventHandler<GenericSingleEventArgs<JsonRpcResponse>>? NotificationReceived;

    /// <summary>
    /// Registers a future request id as awaiting a response. Returns a
    /// task that completes when the matching response arrives or faults
    /// when the supplied <paramref name="cancellationToken"/> fires.
    /// </summary>
    /// <param name="id">The outbound request id.</param>
    /// <param name="cancellationToken">Cancellation cancels the wait.</param>
    /// <returns>The pending response task.</returns>
    public Task<JsonRpcResponse> RegisterPending(long id, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<JsonRpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(id, tcs))
        {
            throw new InvalidOperationException($"Request id {id} is already registered.");
        }

        cancellationToken.Register(() =>
        {
            if (_pending.TryRemove(id, out TaskCompletionSource<JsonRpcResponse>? captured))
            {
                captured.TrySetCanceled(cancellationToken);
            }
        });

        return tcs.Task;
    }

    /// <summary>
    /// Registers an <see cref="IAutoPollSubscription"/> for the supplied
    /// AutoPoll request id. Subsequent inbound responses with that id are
    /// routed to the subscription instead of being treated as one-shot
    /// replies.
    /// </summary>
    /// <param name="id">The AutoPoll request id.</param>
    /// <param name="subscription">The receiver.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="subscription"/> is null.</exception>
    /// <exception cref="InvalidOperationException">If <paramref name="id"/> is already registered.</exception>
    public void RegisterAutoPoll(long id, IAutoPollSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        if (!_autoPolls.TryAdd(id, subscription))
        {
            throw new InvalidOperationException($"AutoPoll id {id} is already registered.");
        }
    }

    /// <summary>
    /// Removes a previously-registered AutoPoll subscription. Returns
    /// <c>true</c> if it was present.
    /// </summary>
    /// <param name="id">The AutoPoll id to remove.</param>
    /// <returns>Whether the subscription existed.</returns>
    public bool UnregisterAutoPoll(long id) => _autoPolls.TryRemove(id, out _);

    /// <summary>
    /// Routes a single inbound JSON-RPC frame. Called by the receive-loop
    /// for each frame the framer emits.
    /// </summary>
    /// <param name="json">The decoded JSON frame text.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="json"/> is null.</exception>
    public void Dispatch(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (json.Length == 0)
        {
            // Empty frame between two terminators — nothing to dispatch.
            return;
        }

        JsonRpcResponse? message;
        try
        {
            message = JsonConvert.DeserializeObject<JsonRpcResponse>(json);
        }
        catch (JsonException ex)
        {
            Log.Error(_deviceId, $"Failed to deserialize inbound JSON-RPC frame: {ex.Message}");
            return;
        }

        if (message is null)
        {
            Log.Error(_deviceId, "Inbound JSON-RPC frame deserialized to null.");
            return;
        }

        // Server-originated notification: no id, populated method.
        if (message.IsNotification)
        {
            NotificationReceived?.Invoke(this, new GenericSingleEventArgs<JsonRpcResponse>(message));
            return;
        }

        if (message.Id is not long id)
        {
            Log.Warn(_deviceId, "Inbound JSON-RPC frame had neither id nor a notification method; discarding.");
            return;
        }

        // AutoPoll push? Check subscriptions first; the same id is reused
        // on every push so a one-shot waiter would never see those.
        if (_autoPolls.TryGetValue(id, out IAutoPollSubscription? subscription))
        {
            subscription.OnPush(message);
            return;
        }

        // One-shot pending response.
        if (_pending.TryRemove(id, out TaskCompletionSource<JsonRpcResponse>? tcs))
        {
            tcs.TrySetResult(message);
            return;
        }

        Log.Warn(_deviceId, $"Inbound JSON-RPC response carries unknown id {id}; discarding.");
    }

    /// <summary>
    /// Cancels every outstanding pending request with the supplied
    /// reason. Called by the connection manager on disconnect so callers
    /// awaiting responses don't hang forever.
    /// </summary>
    /// <param name="reason">The reason to surface as the cancellation message.</param>
    public void CancelAllPending(string reason)
    {
        foreach ((long id, TaskCompletionSource<JsonRpcResponse> tcs) in _pending)
        {
            if (_pending.TryRemove(id, out _))
            {
                tcs.TrySetException(new OperationCanceledException(reason));
            }
        }
    }
}
