// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using gcu_common_utils.GenericEventArgs;
using QscDspDevices.Plugin;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.Protocol;
using QscDspDevices.Transport;

namespace QscDspDevices.Connectivity;

/// <summary>
/// Owns the connection lifecycle of a single Q-SYS Core. Holds the
/// <see cref="IConnectionTransport"/>, drives the
/// <see cref="ConnectionState"/> state machine, and orchestrates the
/// reconnect loop per README §"Device Connection".
/// </summary>
/// <remarks>
/// <para>
/// Fires <see cref="StateChanged"/> on every transition. Callers
/// (notably <c>QscDspTcp</c>) observe this event to flip
/// <c>BaseDevice.IsOnline</c> and call
/// <c>BaseDevice.NotifyOnlineStatus()</c>, in that order, per the
/// README's explicit ordering requirement.
/// </para>
/// <para>
/// Public methods are synchronous so the framework can call them from
/// its main loop without dragging in a SynchronizationContext. All
/// long-running work runs on background tasks.
/// </para>
/// </remarks>
public sealed class ConnectionManager : IDisposable
{
    private readonly string _deviceId;
    private readonly IConnectionTransport _transport;
    private readonly ReconnectStrategy _reconnect;
    private readonly IPostConnectAction _postConnect;
    private readonly CommandQueue _queue;
    private readonly JsonRpcDispatcher _dispatcher;

    private readonly object _stateLock = new();
    private ConnectionState _state = ConnectionState.Disconnected;
    private CancellationTokenSource? _sessionCts;
    private Task? _sessionTask;
    private bool _userRequestedDisconnect;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionManager"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id (used in log messages).</param>
    /// <param name="transport">The transport this manager owns.</param>
    /// <param name="reconnect">The reconnect strategy.</param>
    /// <param name="queue">The command queue this manager toggles on state changes.</param>
    /// <param name="dispatcher">The dispatcher whose pending requests are cancelled on disconnect.</param>
    /// <param name="postConnect">Optional post-connect hook; defaults to a no-op.</param>
    /// <exception cref="ArgumentNullException">If any required argument is null.</exception>
    public ConnectionManager(
        string deviceId,
        IConnectionTransport transport,
        ReconnectStrategy reconnect,
        CommandQueue queue,
        JsonRpcDispatcher dispatcher,
        IPostConnectAction? postConnect = null)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(reconnect);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(dispatcher);

        _deviceId = deviceId;
        _transport = transport;
        _reconnect = reconnect;
        _queue = queue;
        _dispatcher = dispatcher;
        _postConnect = postConnect ?? new NoopPostConnectAction();
    }

    /// <summary>
    /// Raised on every state-machine transition. The event arg carries
    /// the new <see cref="ConnectionState"/>.
    /// </summary>
    public event EventHandler<GenericSingleEventArgs<ConnectionState>>? StateChanged;

    /// <summary>Gets the current state of the connection.</summary>
    public ConnectionState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Begins the connection lifecycle. Idempotent: calling Connect while
    /// already <see cref="ConnectionState.Connecting"/> or
    /// <see cref="ConnectionState.Connected"/> is a no-op.
    /// </summary>
    public void Connect()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_stateLock)
        {
            if (_state == ConnectionState.Connecting || _state == ConnectionState.Connected)
            {
                return;
            }

            _userRequestedDisconnect = false;
            _sessionCts?.Dispose();
            _sessionCts = new CancellationTokenSource();
        }

        CancellationToken token = _sessionCts.Token;
        _sessionTask = Task.Run(() => RunSessionAsync(token), CancellationToken.None);
    }

    /// <summary>
    /// Begins shutdown of the active session. Returns synchronously; the
    /// session is fully torn down asynchronously. <see cref="WaitForDisconnectedAsync"/>
    /// can be awaited to confirm completion.
    /// </summary>
    public void Disconnect()
    {
        if (_disposed)
        {
            return;
        }

        CancellationTokenSource? toCancel;
        lock (_stateLock)
        {
            if (_state == ConnectionState.Disconnected || _state == ConnectionState.Disconnecting)
            {
                return;
            }

            _userRequestedDisconnect = true;
            TransitionLocked(ConnectionState.Disconnecting, "user requested Disconnect()");
            toCancel = _sessionCts;
        }

        try
        {
            toCancel?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Race with Dispose; safe to ignore.
        }
    }

    /// <summary>
    /// Awaits the disconnected state. Useful for tests.
    /// </summary>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <returns>A task that completes when the manager reaches Disconnected.</returns>
    public async Task WaitForDisconnectedAsync(TimeSpan timeout)
    {
        Task? sessionTask = _sessionTask;
        if (sessionTask is null)
        {
            return;
        }

        var deadline = Task.Delay(timeout);
        Task winner = await Task.WhenAny(sessionTask, deadline).ConfigureAwait(false);

        if (winner == deadline && State != ConnectionState.Disconnected)
        {
            throw new TimeoutException(
                $"ConnectionManager did not reach Disconnected within {timeout.TotalSeconds:0.##}s; current state is {State}.");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Disconnect();

        try
        {
            _sessionTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Session task may surface a cancellation as AggregateException
            // when we Dispose during teardown — expected.
        }
        catch (TaskCanceledException)
        {
            // Same rationale.
        }

        _sessionCts?.Dispose();
        _disposed = true;
    }

    private static Task AsTask(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() => tcs.TrySetResult());
        return tcs.Task;
    }

    private async Task RunSessionAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                bool connected = await TryOneAttemptAsync(cancellationToken).ConfigureAwait(false);
                if (connected)
                {
                    // Stay in Connected until the transport reports a fault
                    // or we are cancelled.
                    await WaitForFaultOrCancellationAsync(cancellationToken).ConfigureAwait(false);
                }

                // After a connect failure or a mid-flight disconnect, decide
                // whether to retry per the README's reconnect policy.
                if (cancellationToken.IsCancellationRequested || ShouldStopRetrying())
                {
                    break;
                }

                Log.Notice(_deviceId, $"Reconnect scheduled in {ReconnectStrategy.Interval.TotalSeconds:0}s.");
                try
                {
                    await _reconnect.WaitForNextAttemptAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            FinishSession();
        }
    }

    private async Task<bool> TryOneAttemptAsync(CancellationToken cancellationToken)
    {
        TransitionTo(ConnectionState.Connecting, "starting Connect attempt");

        var connectedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failedTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        EventHandler<EventArgs> onConnected = (_, _) => connectedTcs.TrySetResult(true);
        EventHandler<GenericSingleEventArgs<string>> onFailed = (_, args) => failedTcs.TrySetResult(args.Arg);

        _transport.Connected += onConnected;
        _transport.ConnectionFailed += onFailed;

        try
        {
            _transport.Connect();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(_deviceId, $"Transport.Connect threw unexpectedly: {ex.GetType().Name}: {ex.Message}");
            _transport.Connected -= onConnected;
            _transport.ConnectionFailed -= onFailed;
            return false;
        }

        Task winner = await Task.WhenAny(connectedTcs.Task, failedTcs.Task, AsTask(cancellationToken))
            .ConfigureAwait(false);

        _transport.Connected -= onConnected;
        _transport.ConnectionFailed -= onFailed;

        if (winner == connectedTcs.Task)
        {
            await OnConnectedAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        if (winner == failedTcs.Task)
        {
            string reason = await failedTcs.Task.ConfigureAwait(false);
            Log.Error(_deviceId, $"Connection attempt failed: {reason}");
            return false;
        }

        // Cancellation.
        return false;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The post-connect hook is user-supplied (M3+ hydration). Per README §\"Exception Handling\" the plugin must not crash the host on plug-in faults; the only correct behavior here is log-and-continue.")]
    private async Task OnConnectedAsync(CancellationToken cancellationToken)
    {
        TransitionTo(ConnectionState.Connected, "transport reported Connected");
        _queue.StartAccepting();

        try
        {
            await _postConnect.RunAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(_deviceId, $"Post-connect action threw: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task WaitForFaultOrCancellationAsync(CancellationToken cancellationToken)
    {
        var faultedTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<GenericSingleEventArgs<string>> onFailed = (_, args) => faultedTcs.TrySetResult(args.Arg);

        _transport.ConnectionFailed += onFailed;

        try
        {
            Task winner = await Task.WhenAny(faultedTcs.Task, AsTask(cancellationToken)).ConfigureAwait(false);

            if (winner == faultedTcs.Task)
            {
                string reason = await faultedTcs.Task.ConfigureAwait(false);
                Log.Notice(_deviceId, $"Connection lost: {reason}");
                CleanupAfterDisconnect();

                // Transition out of Connected immediately so observers
                // (notably BaseDevice.IsOnline + NotifyOnlineStatus) see
                // the disconnect right away, not 15 seconds later when
                // we attempt to reconnect. The retry loop in the session
                // will then transition us back through Connecting.
                TransitionTo(ConnectionState.Disconnected, "transport reported a fault");
            }
        }
        finally
        {
            _transport.ConnectionFailed -= onFailed;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Transport.Disconnect on the cleanup path can surface any IO error from the underlying socket; we are tearing down the session and the plugin must not crash the host (README §\"Exception Handling\"). Log Warn and continue.")]
    private void CleanupAfterDisconnect()
    {
        _queue.Drain();
        _dispatcher.CancelAllPending("connection lost");
        try
        {
            _transport.Disconnect();
        }
        catch (Exception ex)
        {
            Log.Warn(_deviceId, $"Transport.Disconnect threw during cleanup: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private bool ShouldStopRetrying()
    {
        lock (_stateLock)
        {
            return _userRequestedDisconnect;
        }
    }

    private void FinishSession()
    {
        CleanupAfterDisconnect();
        TransitionTo(ConnectionState.Disconnected, "session finished");
    }

    private void TransitionTo(ConnectionState next, string cause)
    {
        ConnectionState? from = null;
        bool changed = false;

        lock (_stateLock)
        {
            if (_state == next)
            {
                return;
            }

            from = _state;
            _state = next;
            changed = true;
        }

        if (changed)
        {
            Log.Notice(_deviceId, $"Connection state {from} -> {next} ({cause}).");
            StateChanged?.Invoke(this, new GenericSingleEventArgs<ConnectionState>(next));
        }
    }

    private void TransitionLocked(ConnectionState next, string cause)
    {
        // Caller already holds _stateLock.
        if (_state == next)
        {
            return;
        }

        ConnectionState from = _state;
        _state = next;
        Log.Notice(_deviceId, $"Connection state {from} -> {next} ({cause}).");

        // Fire the event outside the lock to avoid re-entrancy from
        // subscribers (they should not hold the lock).
        var args = new GenericSingleEventArgs<ConnectionState>(next);
        ThreadPool.QueueUserWorkItem(_ => StateChanged?.Invoke(this, args));
    }
}
