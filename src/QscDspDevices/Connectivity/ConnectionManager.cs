// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using gcu_common_utils.GenericEventArgs;
using QscDspDevices.Plugin;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.JsonRpc;
using QscDspDevices.Protocol.Logging;
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
    /// <summary>The keepalive tick cadence (1 s; the timer itself enforces the 30 s silence window).</summary>
    public static readonly TimeSpan KeepaliveTickInterval = TimeSpan.FromSeconds(1);

    private readonly string _deviceId;
    private readonly IConnectionTransport _transport;
    private readonly ReconnectStrategy _reconnect;
    private readonly IPostConnectAction _postConnect;
    private readonly CommandQueue _queue;
    private readonly JsonRpcDispatcher _dispatcher;
    private readonly ThreadCensus _threadCensus;
    private readonly IQrcClock? _clock;
    private readonly IdGenerator? _ids;

    private readonly object _stateLock = new();
    private ConnectionState _state = ConnectionState.Disconnected;
    private CancellationTokenSource? _sessionCts;
    private Task? _sessionTask;
    private CancellationTokenSource? _ioCts;
    private Task? _sendLoopTask;
    private Task? _keepaliveTask;
    private KeepaliveTimer? _keepalive;
    private QrcFramer? _framer;
    private EventHandler<GenericSingleEventArgs<ReadOnlyMemory<byte>>>? _onRxBytes;
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
    /// <param name="threadCensus">Optional shared thread census. Defaults to a fresh census tagged to <paramref name="deviceId"/>; pass an existing instance when multiple components (e.g. <c>QscDspTcp</c> and the manager) must share one budget.</param>
    /// <param name="clock">Optional clock for the M3 keepalive timer. When supplied alongside <paramref name="ids"/>, the manager wires a 30 s keepalive that emits <c>NoOp</c> requests during outbound silence. M2 unit tests pass null on both to disable keepalive.</param>
    /// <param name="ids">Optional id generator for the keepalive's outbound NoOp requests. See <paramref name="clock"/>.</param>
    /// <exception cref="ArgumentNullException">If any required argument is null.</exception>
    public ConnectionManager(
        string deviceId,
        IConnectionTransport transport,
        ReconnectStrategy reconnect,
        CommandQueue queue,
        JsonRpcDispatcher dispatcher,
        IPostConnectAction? postConnect = null,
        ThreadCensus? threadCensus = null,
        IQrcClock? clock = null,
        IdGenerator? ids = null)
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
        _threadCensus = threadCensus ?? new ThreadCensus(deviceId);

        // Clock + id-generator are optional because the M2 tests construct
        // a manager without them; when both are supplied the M3 keepalive
        // timer gets wired during OnConnectedAsync. Without them, the
        // session runs without keepalive — fine for the deterministic-clock
        // unit tests but disabled in production unless QscDspTcp passes
        // them through.
        _clock = clock;
        _ids = ids;
    }

    /// <summary>
    /// Raised on every state-machine transition. The event arg carries
    /// the new <see cref="ConnectionState"/>.
    /// </summary>
    public event EventHandler<GenericSingleEventArgs<ConnectionState>>? StateChanged;

    /// <summary>
    /// Gets the runtime thread-budget guard. Exposed for tests and for
    /// future milestones that spawn additional plugin-owned threads.
    /// </summary>
    public ThreadCensus ThreadCensus => _threadCensus;

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
            toCancel = _sessionCts;
        }

        // Fire the Disconnecting transition AFTER releasing the lock so
        // the event raise is synchronous (matching TransitionTo's shape)
        // and observers cannot see Disconnected before Disconnecting.
        TransitionTo(ConnectionState.Disconnecting, "user requested Disconnect()");

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
        _ioCts?.Dispose();
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
        // Register the session task with the thread census so the plugin
        // honours the README §4 3-thread budget at runtime, not just by
        // convention. The token-based API is essential here because this
        // method awaits across threadpool worker boundaries — keying on
        // Environment.CurrentManagedThreadId would Unregister the wrong
        // id when the continuation resumes on a different thread.
        // M3 adds dedicated send/receive/timer threads, each carrying
        // their own registration handle on this same census.
        ThreadCensusRegistration registration = _threadCensus.Register("session");

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
            registration.Dispose();
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
        StartIoLoops(cancellationToken);

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

    private void StartIoLoops(CancellationToken sessionToken)
    {
        // Tear down any leftover I/O state from a prior session iteration
        // BEFORE wiring fresh hooks. Belt-and-braces against a missed
        // CleanupAfterDisconnect during a buggy reconnect.
        StopIoLoops();

        _framer = new QrcFramer();
        _ioCts = CancellationTokenSource.CreateLinkedTokenSource(sessionToken);

        // Receive path: every batch of bytes from the transport feeds the
        // framer; complete frames go straight to the dispatcher. The
        // event fires on whatever thread the transport's underlying
        // socket reports — RawTcpTransport posts to the threadpool.
        _onRxBytes = (_, args) =>
        {
            QrcFramer? framer = _framer;
            if (framer is null)
            {
                return;
            }

            try
            {
                foreach (string frame in framer.Append(args.Arg.Span))
                {
                    DispatchFrameSafely(frame);
                }
            }
            catch (FrameTooLargeException ex)
            {
                Log.Error(_deviceId, $"Inbound frame exceeded max size: {ex.Message}. Dropping connection.");
                _transport.Disconnect();
            }
        };
        _transport.RxReceived += _onRxBytes;

        // Send path: a single task drains the queue and writes to the
        // transport. Stays alive until the session-level cancellation
        // token fires (Disconnect or session end).
        CancellationToken ioToken = _ioCts.Token;
        _sendLoopTask = Task.Run(() => RunSendLoopAsync(ioToken), ioToken);

        // Keepalive: only when both clock + id generator were supplied.
        // The M2 unit tests don't supply them and intentionally run
        // without keepalive.
        if (_clock is not null && _ids is not null)
        {
            _keepalive = new KeepaliveTimer(_clock, _ids, EnqueueAsync);
            _keepaliveTask = Task.Run(() => RunKeepaliveLoopAsync(ioToken), ioToken);
        }
    }

    private ValueTask<bool> EnqueueAsync(JsonRpcRequest request)
        => ValueTask.FromResult(_queue.TryEnqueue(request));

    /// <summary>
    /// Wraps a single <see cref="JsonRpcDispatcher.Dispatch"/> call so a
    /// misbehaving framework-side event subscriber (which the rx-thread
    /// chain ultimately invokes synchronously through the AutoPoll
    /// fanout) cannot escape and propagate into the
    /// <c>BasicTcpClient</c> rx event chain — that would crash the
    /// host, violating the README's "the plugin must not crash the
    /// host" rule.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Per README §\"Exception Handling\", the plugin must not crash the host. The rx-thread chain ultimately invokes user-code event handlers (IAudioControl, IAudioRoutable, IAudioZoneEnabler, IDspLogicTriggerSupport); a throw from any of those would otherwise escape into the BasicTcpClient rx callback. Log Error and continue.")]
    private void DispatchFrameSafely(string frame)
    {
        try
        {
            _dispatcher.Dispatch(frame);
        }
        catch (Exception ex)
        {
            Log.Error(
                _deviceId,
                $"Inbound frame dispatch threw {ex.GetType().Name}: {ex.Message}. Continuing on the receive thread.");
        }
    }

    private async Task RunKeepaliveLoopAsync(CancellationToken cancellationToken)
    {
        // Same rationale as RunSendLoopAsync — register the M3 keepalive
        // Task so the README §4 ≤3-thread budget accounts for it.
        ThreadCensusRegistration registration = _threadCensus.Register("keepalive");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(KeepaliveTickInterval, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                KeepaliveTimer? timer = _keepalive;
                if (timer is null)
                {
                    return;
                }

                await timer.TickAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Session shutting down; expected.
        }
        finally
        {
            registration.Dispose();
        }
    }

    private async Task RunSendLoopAsync(CancellationToken cancellationToken)
    {
        // Register with the thread census so the runtime guard accounts
        // for the M3 send Task as one of the README §4 "≤3 plugin
        // threads". The token-based registration is essential: this
        // method awaits CommandQueue.DequeueAsync, so its continuations
        // resume on different threadpool workers.
        ThreadCensusRegistration registration = _threadCensus.Register("send");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                JsonRpcRequest request;
                try
                {
                    request = await _queue.DequeueAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                byte[] payload = QrcFramer.Encode(json);

                // Logger.Debug is off by default at runtime; turning it on
                // for diagnostics must NOT expose Logon credentials to the
                // log stream. RedactingDebugFormatter formats the request
                // with Logon's Password field replaced by "***" before
                // logging; non-Logon requests pass through verbatim.
                Log.Debug(_deviceId, RedactingDebugFormatter.Format(request));

                try
                {
                    _transport.Send(payload);
                    _keepalive?.NotifyOutboundSent();
                }
                catch (InvalidOperationException ex)
                {
                    Log.Warn(_deviceId, $"Send refused: {ex.Message}. Dropping pending request id={request.Id}.");
                    return;
                }
                catch (System.IO.IOException ex)
                {
                    Log.Warn(_deviceId, $"Send failed: {ex.Message}. Dropping pending request id={request.Id}.");
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Session shutting down; expected.
        }
        finally
        {
            registration.Dispose();
        }
    }

    private void StopIoLoops()
    {
        if (_onRxBytes is not null)
        {
            _transport.RxReceived -= _onRxBytes;
            _onRxBytes = null;
        }

        try
        {
            _ioCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already torn down.
        }

        try
        {
            _sendLoopTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Cancellation noise.
        }

        try
        {
            _keepaliveTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Cancellation noise.
        }

        _sendLoopTask = null;
        _keepaliveTask = null;
        _keepalive = null;
        _ioCts?.Dispose();
        _ioCts = null;
        _framer = null;
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
        StopIoLoops();
        _queue.Drain();
        _dispatcher.CancelAllPending("connection lost");

        // Each reconnect issues a fresh ChangeGroup.AutoPoll with a new
        // id; without clearing here, the dispatcher accumulates a stale
        // subscription per reconnect cycle (memory leak + a CA1031-
        // swallowed InvalidOperationException on a future id wrap).
        // Race: StopIoLoops above detached the rx handler, but .NET's
        // event -= does not interrupt an in-flight invocation. A push
        // that entered the dispatcher microseconds earlier may still
        // deliver a delta to a callback after the rx detach. The
        // dispatcher's ConcurrentDictionary makes this safe; one stale
        // delta is tolerable and the next-hydration AutoPoll reconciles
        // the cache with the Core's actual state.
        int cleared = _dispatcher.ClearAutoPolls();
        if (cleared > 0)
        {
            Log.Notice(_deviceId, $"Cleared {cleared} stale AutoPoll subscription(s) on disconnect.");
        }

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
}
