// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.Connectivity.PostConnect;

/// <summary>
/// Post-connect action that issues a <c>Logon</c> JSON-RPC request
/// with the configured credentials. Skipped (returns <c>Task.CompletedTask</c>)
/// when both fields of the supplied <see cref="LogonCredentials"/> are
/// empty — the QSC Core treats Logon as optional unless the design
/// has Access Control configured (<c>research/QRC_PROTOCOL.md</c> §2).
/// </summary>
/// <remarks>
/// <para>
/// On a non-success response (error code 10 or other), the action logs
/// <c>Logger.Warn</c> and returns successfully — the
/// <c>HydrateChangeGroupAction</c> still runs, and any privileged
/// commands that subsequently fail with code 10 will surface the
/// authentication problem at the use-site. We don't retry Logon here
/// because the QSC Core docs note that wrong credentials don't drop
/// the TCP connection; it just keeps refusing privileged commands.
/// </para>
/// <para>
/// The credentials are read via a callback rather than passed by value
/// so the user's config-time state (mutable until next Initialize) is
/// always read fresh on every reconnect. This avoids the
/// "credentials changed but the post-connect action still uses the
/// old ones" footgun on a long-lived session.
/// </para>
/// </remarks>
public sealed class LogonAction : IPostConnectAction
{
    /// <summary>The QSC Logon response timeout (5 s; see protocol research §2).</summary>
    public static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(5);

    private readonly string _deviceId;
    private readonly Func<LogonCredentials> _credentialsSource;
    private readonly CommandQueue _queue;
    private readonly JsonRpcDispatcher _dispatcher;
    private readonly IdGenerator _ids;
    private TaskCompletionSource<bool>? _completion;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogonAction"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id, used in log messages.</param>
    /// <param name="credentialsSource">A delegate returning the latest credentials. Re-read on every RunAsync.</param>
    /// <param name="queue">The command queue to enqueue the Logon request on.</param>
    /// <param name="dispatcher">The JSON-RPC dispatcher (for response correlation).</param>
    /// <param name="ids">The shared monotonic id generator.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public LogonAction(
        string deviceId,
        Func<LogonCredentials> credentialsSource,
        CommandQueue queue,
        JsonRpcDispatcher dispatcher,
        IdGenerator ids)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(credentialsSource);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(ids);

        _deviceId = deviceId;
        _credentialsSource = credentialsSource;
        _queue = queue;
        _dispatcher = dispatcher;
        _ids = ids;
    }

    /// <summary>
    /// Gets a task that completes when the action's last <c>RunAsync</c>
    /// invocation has either issued a successful Logon, decided to skip,
    /// or logged a Logon error. <see cref="HydrateChangeGroupAction"/>
    /// awaits this so the change-group subscribe never races ahead of
    /// Logon completion.
    /// </summary>
    /// <returns>A task that resolves when the most recent RunAsync has settled.</returns>
    public Task<bool> WaitForCompletionAsync()
    {
        TaskCompletionSource<bool>? c = Volatile.Read(ref _completion);
        return c?.Task ?? Task.FromResult(true);
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var newCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Volatile.Write(ref _completion, newCompletion);

        try
        {
            LogonCredentials creds = _credentialsSource();
            if (!creds.IsConfigured)
            {
                Log.Notice(_deviceId, "Logon credentials not configured; skipping Logon (Core must accept anonymous QRC).");
                newCompletion.TrySetResult(true);
                return;
            }

            long id = _ids.Next();
            var request = new JsonRpcRequest
            {
                Id = id,
                Method = "Logon",
                Params = new { User = creds.Username ?? string.Empty, Password = creds.Password ?? string.Empty },
            };

            Task<JsonRpcResponse> pending = _dispatcher.RegisterPending(id, cancellationToken);

            if (!_queue.TryEnqueue(request))
            {
                Log.Error(_deviceId, "Logon could not be enqueued (queue not accepting). Continuing without Logon.");
                newCompletion.TrySetResult(false);
                return;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ResponseTimeout);

            try
            {
                JsonRpcResponse response = await pending.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
                if (response.IsError)
                {
                    Log.Warn(
                        _deviceId,
                        $"Logon returned error {response.Error?.Code} '{response.Error?.Message}'. Subsequent privileged commands may be refused with code 10.");
                    newCompletion.TrySetResult(false);
                }
                else
                {
                    Log.Notice(_deviceId, "Logon succeeded.");
                    newCompletion.TrySetResult(true);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                Log.Warn(_deviceId, $"Logon timed out after {ResponseTimeout.TotalSeconds:0}s. Continuing without Logon.");
                newCompletion.TrySetResult(false);
            }
        }
        catch (OperationCanceledException)
        {
            newCompletion.TrySetCanceled(cancellationToken);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // CA1031 suppression: this method is the post-connect hook and
            // MUST NOT propagate exceptions; the connection-manager would
            // otherwise drop the connection on a Logon-side bug. Log,
            // mark the completion failed, and let HydrateChangeGroupAction
            // proceed.
            Log.Warn(_deviceId, $"Logon action threw {ex.GetType().Name}: {ex.Message}.");
            newCompletion.TrySetResult(false);
        }
    }
}
