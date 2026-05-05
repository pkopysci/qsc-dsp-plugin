// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using gcu_common_utils.GenericEventArgs;
using QscDspDevices.Plugin;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.Ecp;
using QscDspDevices.Transport;

namespace QscDspDevices.Connectivity.Ecp;

/// <summary>
/// ECP-side equivalent of <see cref="ConnectionManager"/>. Owns the
/// connect / connecting / connected / disconnecting / disconnected
/// state machine for an ECP transport, plus the three steady-state
/// task-loops (session, send, keepalive) registered with
/// <see cref="ThreadCensus"/>. The receive path is event-driven on
/// the transport's <see cref="IConnectionTransport.RxReceived"/>
/// callback (not plugin-owned, not counted toward the budget).
/// </summary>
/// <remarks>
/// Per design.md §D-E4, this is duplication-by-copy from the QRC
/// <see cref="ConnectionManager"/>: the divergence (login_required
/// banner handling, sg keepalive, no post-connect Logon JSON-RPC
/// chain) is large enough that a generic abstraction would carry
/// more cost than the duplication itself.
/// </remarks>
internal sealed class EcpConnectionManager : IDisposable
{
    private static readonly TimeSpan KeepaliveTickInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan KeepaliveSilenceWindow = TimeSpan.FromSeconds(30);

    private readonly string _deviceId;
    private readonly IConnectionTransport _transport;
    private readonly EcpCommandQueue _queue;
    private readonly EcpDispatcher _dispatcher;
    private readonly EcpFramer _framer = new();
    private readonly ReconnectStrategy _reconnect;
    private readonly ThreadCensus _threadCensus;
    private readonly Func<EcpCredentials?> _credentialsSource;
    private readonly Action? _postAuthHook;

    private readonly object _stateLock = new();
    private ConnectionState _state = ConnectionState.Disconnected;
    private CancellationTokenSource? _sessionCts;
    private CancellationTokenSource? _ioCts;
    private Task? _sessionTask;
    private Task? _sendLoopTask;
    private Task? _keepaliveTask;
    private TaskCompletionSource? _disconnectedSignal;
    private DateTime _lastOutboundUtc = DateTime.UtcNow;
    private bool _userRequestedDisconnect;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpConnectionManager"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="transport">The transport.</param>
    /// <param name="reconnect">Reconnect cadence policy.</param>
    /// <param name="queue">The outbound command queue.</param>
    /// <param name="dispatcher">The inbound dispatcher.</param>
    /// <param name="credentialsSource">Lookup of <see cref="EcpCredentials"/> at login time. Returns null for anonymous Cores.</param>
    /// <param name="threadCensus">Optional census; one is created internally if omitted.</param>
    /// <param name="postAuthHook">Optional callback invoked once per connect cycle, after auth completes and the queue is accepting (M-ECP-part-3 wires <c>EcpHydrateAction</c> here).</param>
    /// <exception cref="ArgumentNullException">If any required argument is null.</exception>
    public EcpConnectionManager(
        string deviceId,
        IConnectionTransport transport,
        ReconnectStrategy reconnect,
        EcpCommandQueue queue,
        EcpDispatcher dispatcher,
        Func<EcpCredentials?> credentialsSource,
        ThreadCensus? threadCensus = null,
        Action? postAuthHook = null)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(reconnect);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(credentialsSource);

        _deviceId = deviceId;
        _transport = transport;
        _reconnect = reconnect;
        _queue = queue;
        _dispatcher = dispatcher;
        _credentialsSource = credentialsSource;
        _postAuthHook = postAuthHook;
        _threadCensus = threadCensus ?? new ThreadCensus(deviceId);
    }

    /// <summary>Raised when the manager's <see cref="State"/> changes.</summary>
    public event EventHandler<GenericSingleEventArgs<ConnectionState>>? StateChanged;

    /// <summary>Gets the dispatcher, exposed for service-tier wiring.</summary>
    public EcpDispatcher Dispatcher => _dispatcher;

    /// <summary>Gets the command queue, exposed for service-tier wiring.</summary>
    public EcpCommandQueue Queue => _queue;

    /// <summary>Gets the transport, exposed for tests and the redundancy path.</summary>
    public IConnectionTransport Transport => _transport;

    /// <summary>Gets the thread census, exposed for tests.</summary>
    public ThreadCensus ThreadCensus => _threadCensus;

    /// <summary>Gets the current state.</summary>
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

    /// <summary>Starts the connect lifecycle.</summary>
    /// <exception cref="ObjectDisposedException">If the manager has been disposed.</exception>
    /// <exception cref="InvalidOperationException">If <see cref="Connect"/> has already been called.</exception>
    public void Connect()
    {
        lock (_stateLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_sessionTask is not null)
            {
                throw new InvalidOperationException("Already connecting or connected.");
            }

            _userRequestedDisconnect = false;
            _sessionCts = new CancellationTokenSource();
            _disconnectedSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        CancellationToken token = _sessionCts!.Token;
        _sessionTask = Task.Run(() => RunSessionAsync(token), CancellationToken.None);
    }

    /// <summary>Stops the session and tears down the I/O loops.</summary>
    public void Disconnect()
    {
        lock (_stateLock)
        {
            if (_disposed || _state == ConnectionState.Disconnected || _state == ConnectionState.Disconnecting)
            {
                return;
            }

            _userRequestedDisconnect = true;
        }

        TransitionTo(ConnectionState.Disconnecting, "user requested Disconnect()");

        try
        {
            CancelSafely(_sessionCts);
        }
        catch (ObjectDisposedException)
        {
            // Already torn down; ignore.
        }
    }

    /// <summary>
    /// Awaits until the manager reaches <see cref="ConnectionState.Disconnected"/>
    /// or the supplied timeout elapses.
    /// </summary>
    /// <param name="timeout">The maximum wait.</param>
    /// <returns>A task that completes when the manager reaches Disconnected.</returns>
    public async Task WaitForDisconnectedAsync(TimeSpan timeout)
    {
        TaskCompletionSource? signal;
        lock (_stateLock)
        {
            if (_state == ConnectionState.Disconnected)
            {
                return;
            }

            signal = _disconnectedSignal;
        }

        if (signal is null)
        {
            return;
        }

        Task winner = await Task.WhenAny(signal.Task, Task.Delay(timeout)).ConfigureAwait(false);
        if (winner != signal.Task)
        {
            throw new TimeoutException($"EcpConnectionManager did not reach Disconnected within {timeout.TotalSeconds:0.##}s; current state is {State}.");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_stateLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        Disconnect();
        try
        {
            _sessionTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Swallow shutdown exceptions; the session task is going away.
        }

        _sessionCts?.Dispose();
        _ioCts?.Dispose();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Session orchestrator must not crash the host on transport / framer / dispatcher faults (README §\"Exception Handling\"). Log Error and let the reconnect loop take over.")]
    private async Task RunSessionAsync(CancellationToken cancellationToken)
    {
        ThreadCensusRegistration registration = _threadCensus.Register("session");
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TransitionTo(ConnectionState.Connecting, "starting connect attempt");

                try
                {
                    // Subscribe the auth observer BEFORE TryConnectAsync
                    // so the dispatcher already has it when the rx
                    // pipeline begins delivering bytes (TryConnectAsync
                    // wires the rx subscription, which then forwards to
                    // the dispatcher). Without this ordering, a server
                    // that pushes `login_required` immediately on accept
                    // can land the banner before we subscribe.
                    EcpCredentials? creds = _credentialsSource();
                    TaskCompletionSource<bool>? authOutcome = creds is null ? null : new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    EventHandler<gcu_common_utils.GenericEventArgs.GenericSingleEventArgs<EcpResponse>>? authHandler = null;
                    if (creds is not null && authOutcome is not null)
                    {
                        authHandler = MakeAuthHandler(creds, authOutcome);
                        _dispatcher.ResponseReceived += authHandler;
                    }

                    bool connected = await TryConnectAsync(cancellationToken).ConfigureAwait(false);
                    if (!connected)
                    {
                        if (authHandler is not null)
                        {
                            _dispatcher.ResponseReceived -= authHandler;
                        }

                        await DelayBeforeReconnectAsync(cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    StartIoLoops(cancellationToken);

                    if (authOutcome is not null)
                    {
                        bool authed = await WaitForAuthAsync(authOutcome.Task, cancellationToken).ConfigureAwait(false);
                        if (authHandler is not null)
                        {
                            _dispatcher.ResponseReceived -= authHandler;
                        }

                        if (!authed)
                        {
                            Log.Error(_deviceId, "ECP authentication failed; reconnecting after the standard interval.");
                            CleanupAfterDisconnect();
                            await DelayBeforeReconnectAsync(cancellationToken).ConfigureAwait(false);
                            continue;
                        }
                    }

                    TransitionTo(ConnectionState.Connected, "transport up + auth complete");
                    _queue.StartAccepting();
                    _postAuthHook?.Invoke();

                    await WaitForFaultOrDisconnectAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(_deviceId, $"ECP session error: {ex.GetType().Name}: {ex.Message}");
                }

                CleanupAfterDisconnect();

                if (_userRequestedDisconnect)
                {
                    break;
                }

                await DelayBeforeReconnectAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            registration.Dispose();
            TransitionTo(ConnectionState.Disconnected, "session task exited");
            lock (_stateLock)
            {
                _disconnectedSignal?.TrySetResult();
            }
        }
    }

    private async Task<bool> TryConnectAsync(CancellationToken cancellationToken)
    {
        // Subscribe to Connected/RxReceived BEFORE calling Connect.
        // Two races to defeat:
        //  (a) StubTransport: the test thread can call
        //      SimulateConnectSuccess between TransitionTo(Connecting)
        //      and our subscription. Mitigated by the IsConnected
        //      backstop below.
        //  (b) RawTcpTransport: the server may push an immediate
        //      banner (e.g., login_required) on accept; the read
        //      loop fires RxReceived as soon as bytes arrive, and
        //      with no subscriber the bytes are silently lost.
        //      Mitigated by subscribing onRx here so the framer +
        //      dispatcher see the banner before StartIoLoops takes
        //      ownership.
        var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<EventArgs> onConnected = (_, _) => connected.TrySetResult(true);
        EventHandler<GenericSingleEventArgs<string>> onFailed = (_, args) =>
        {
            Log.Warn(_deviceId, $"ECP transport connect failed: {args.Arg}");
            connected.TrySetResult(false);
        };

        _transport.Connected += onConnected;
        _transport.ConnectionFailed += onFailed;
        _transport.RxReceived += OnRxReceived;

        try
        {
            if (_transport.IsConnected)
            {
                connected.TrySetResult(true);
            }

            using CancellationTokenRegistration registration = cancellationToken.Register(() => connected.TrySetResult(false));
            _transport.Connect();
            return await connected.Task.ConfigureAwait(false) && !cancellationToken.IsCancellationRequested;
        }
        finally
        {
            _transport.Connected -= onConnected;
            _transport.ConnectionFailed -= onFailed;

            // RxReceived stays subscribed past TryConnectAsync;
            // CleanupAfterDisconnect removes it on each disconnect.
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The rx-event chain ultimately invokes user-supplied service callbacks; per README §\"Exception Handling\" the plugin must not crash the host on a misbehaving callback.")]
    private void OnRxReceived(object? sender, GenericSingleEventArgs<ReadOnlyMemory<byte>> args)
    {
        try
        {
            foreach (string frame in _framer.Append(args.Arg.Span))
            {
                try
                {
                    _dispatcher.Dispatch(frame);
                }
                catch (Exception ex)
                {
                    Log.Error(_deviceId, $"ECP inbound dispatch threw {ex.GetType().Name}: {ex.Message}. Continuing.");
                }
            }
        }
        catch (FrameTooLargeException ex)
        {
            Log.Error(_deviceId, $"ECP frame exceeded max size: {ex.Message}. Dropping connection.");
            CancelSafely(_ioCts);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Auth handler runs on the receive callback path; per README §\"Exception Handling\" the plugin must not crash the host on a transport / framer fault while sending the login command. Log Error and let the reconnect cycle take over.")]
    private EventHandler<gcu_common_utils.GenericEventArgs.GenericSingleEventArgs<EcpResponse>> MakeAuthHandler(EcpCredentials creds, TaskCompletionSource<bool> outcome)
        => (_, args) =>
        {
            switch (args.Arg.Kind)
            {
                case EcpResponseKind.LoginRequired:
                    try
                    {
                        _transport.Send(EcpFramer.Encode(EcpCommand.Login(creds.Username, creds.Pin)));
                        _lastOutboundUtc = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(_deviceId, $"ECP login send threw {ex.GetType().Name}: {ex.Message}.");
                        outcome.TrySetResult(false);
                    }

                    break;
                case EcpResponseKind.LoginSuccess:
                    outcome.TrySetResult(true);
                    break;
                case EcpResponseKind.LoginFailed:
                    Log.Error(_deviceId, "ECP login_failed; Core will close the socket.");
                    outcome.TrySetResult(false);
                    break;
            }
        };

#pragma warning disable SA1204 // Static members should appear before non-static members
    private static async Task<bool> WaitForAuthAsync(Task<bool> outcomeTask, CancellationToken cancellationToken)
    {
        Task winner = await Task.WhenAny(outcomeTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken)).ConfigureAwait(false);
        if (winner == outcomeTask)
        {
            return await outcomeTask.ConfigureAwait(false);
        }

        return !cancellationToken.IsCancellationRequested;
    }
#pragma warning restore SA1204

    private async Task DelayBeforeReconnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _reconnect.WaitForNextAttemptAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cancelled — fall through to terminate the loop.
        }
    }

    private void StartIoLoops(CancellationToken sessionToken)
    {
        _ioCts?.Dispose();
        _ioCts = CancellationTokenSource.CreateLinkedTokenSource(sessionToken);
        CancellationToken ioToken = _ioCts.Token;

        // RxReceived was subscribed in TryConnectAsync (see comment
        // there); we don't re-subscribe here. The send + keepalive
        // loops own their own ThreadCensus registrations.
        _sendLoopTask = Task.Run(() => RunSendLoopAsync(ioToken), ioToken);
        _keepaliveTask = Task.Run(() => RunKeepaliveLoopAsync(ioToken), ioToken);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Send loop must not crash the host on transport faults; Log Warn and let the reconnect cycle take over (README §\"Exception Handling\").")]
    private async Task RunSendLoopAsync(CancellationToken cancellationToken)
    {
        ThreadCensusRegistration registration = _threadCensus.Register("send");
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string command;
                try
                {
                    command = await _queue.DequeueAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                try
                {
                    byte[] bytes = EcpFramer.Encode(command);
                    _transport.Send(bytes);
                    _lastOutboundUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    Log.Warn(_deviceId, $"ECP send refused: {ex.Message}.");
                    CancelSafely(_ioCts);
                    break;
                }
            }
        }
        finally
        {
            registration.Dispose();
        }
    }

    private async Task RunKeepaliveLoopAsync(CancellationToken cancellationToken)
    {
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

                if (DateTime.UtcNow - _lastOutboundUtc < KeepaliveSilenceWindow)
                {
                    continue;
                }

                _queue.TryEnqueue(EcpCommand.StatusGet());
            }
        }
        finally
        {
            registration.Dispose();
        }
    }

    private async Task WaitForFaultOrDisconnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.WhenAny(_sendLoopTask ?? Task.CompletedTask, _keepaliveTask ?? Task.CompletedTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown initiated; fall through.
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Cleanup runs on the disconnect path; tearing down a transport / cancellation token can surface any IO error. Log Warn and continue (README §\"Exception Handling\").")]
    private void CleanupAfterDisconnect()
    {
        // Unsubscribe rx so the next connect attempt re-subscribes
        // fresh (TryConnectAsync owns the subscription lifecycle).
        _transport.RxReceived -= OnRxReceived;

        try
        {
            CancelSafely(_ioCts);
        }
        catch (Exception ex)
        {
            Log.Warn(_deviceId, $"EcpConnectionManager io-cancel threw {ex.GetType().Name}: {ex.Message}.");
        }

        _queue.Drain();

        try
        {
            _transport.Disconnect();
        }
        catch (Exception ex)
        {
            Log.Warn(_deviceId, $"EcpConnectionManager transport.Disconnect threw {ex.GetType().Name}: {ex.Message}.");
        }

        try
        {
            _sendLoopTask?.Wait(TimeSpan.FromSeconds(2));
            _keepaliveTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Tasks may have thrown on cancellation; safe to ignore here.
        }

        _sendLoopTask = null;
        _keepaliveTask = null;
    }

    private void TransitionTo(ConnectionState next, string reason)
    {
        bool fire;
        lock (_stateLock)
        {
            if (_state == next)
            {
                return;
            }

            _state = next;
            fire = true;
            _ = reason;
        }

        if (fire)
        {
            StateChanged?.Invoke(this, new GenericSingleEventArgs<ConnectionState>(next));
        }
    }

#pragma warning disable SA1204 // Static members should appear before non-static members
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA1849:Call async methods when in an async method",
        Justification = "CancelSafely is intentionally synchronous; CA1849 flares because callers live in async methods, but Cancel itself does not block.")]
    private static void CancelSafely(CancellationTokenSource? cts) => cts?.Cancel();
#pragma warning restore SA1204
}
