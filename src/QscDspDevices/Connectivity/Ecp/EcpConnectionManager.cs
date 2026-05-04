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
    /// <exception cref="ArgumentNullException">If any required argument is null.</exception>
    public EcpConnectionManager(
        string deviceId,
        IConnectionTransport transport,
        ReconnectStrategy reconnect,
        EcpCommandQueue queue,
        EcpDispatcher dispatcher,
        Func<EcpCredentials?> credentialsSource,
        ThreadCensus? threadCensus = null)
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
                    bool connected = await TryConnectAsync(cancellationToken).ConfigureAwait(false);
                    if (!connected)
                    {
                        await DelayBeforeReconnectAsync(cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    StartIoLoops(cancellationToken);

                    // ECP §2: the Core may send `login_required` immediately
                    // on accept or in reply to the first command. We give it
                    // 500ms to surface the banner; absence is treated as
                    // anonymous-mode and we proceed straight to Accepting.
                    bool authed = await TryAuthenticateAsync(cancellationToken).ConfigureAwait(false);
                    if (!authed)
                    {
                        // login_failed already logged at Error; tear down and
                        // let the M2 reconnect cycle take over.
                        Log.Error(_deviceId, "ECP authentication failed; reconnecting after the standard interval.");
                        CleanupAfterDisconnect();
                        await DelayBeforeReconnectAsync(cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    TransitionTo(ConnectionState.Connected, "transport up + auth complete");
                    _queue.StartAccepting();

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
        var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<EventArgs> onConnected = (_, _) => connected.TrySetResult(true);
        EventHandler<GenericSingleEventArgs<string>> onFailed = (_, args) =>
        {
            Log.Warn(_deviceId, $"ECP transport connect failed: {args.Arg}");
            connected.TrySetResult(false);
        };

        _transport.Connected += onConnected;
        _transport.ConnectionFailed += onFailed;

        try
        {
            using CancellationTokenRegistration registration = cancellationToken.Register(() => connected.TrySetResult(false));
            _transport.Connect();
            return await connected.Task.ConfigureAwait(false) && !cancellationToken.IsCancellationRequested;
        }
        finally
        {
            _transport.Connected -= onConnected;
            _transport.ConnectionFailed -= onFailed;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Auth handler runs on the receive callback path; per README §\"Exception Handling\" the plugin must not crash the host on a transport / framer fault while sending the login command. Log Error and let the reconnect cycle take over.")]
    private async Task<bool> TryAuthenticateAsync(CancellationToken cancellationToken)
    {
        // Subscribe to the dispatcher with a TCS that completes when we
        // see one of: login_required (we send the login), login_success
        // (auth done), or login_failed (auth failed). Anonymous-mode
        // Cores never send a banner; in that case we time out the
        // 500ms wait and proceed.
        var outcome = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<gcu_common_utils.GenericEventArgs.GenericSingleEventArgs<EcpResponse>> handler = (_, args) =>
        {
            switch (args.Arg.Kind)
            {
                case EcpResponseKind.LoginRequired:
                    EcpCredentials? creds = _credentialsSource();
                    if (creds is null)
                    {
                        Log.Error(_deviceId, "ECP Core sent login_required but no credentials configured.");
                        outcome.TrySetResult(false);
                        return;
                    }

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

        _dispatcher.ResponseReceived += handler;
        try
        {
            // Race the auth outcome against a 500ms anonymous-mode
            // window. If neither arrives, treat the connection as
            // anonymous and proceed.
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            using (linkedCts.Token.Register(() => outcome.TrySetResult(true)))
            {
                return await outcome.Task.ConfigureAwait(false);
            }
        }
        finally
        {
            _dispatcher.ResponseReceived -= handler;
        }
    }

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

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The rx-event chain ultimately invokes user-supplied service callbacks; per README §\"Exception Handling\" the plugin must not crash the host on a misbehaving callback.")]
    private void StartIoLoops(CancellationToken sessionToken)
    {
        _ioCts?.Dispose();
        _ioCts = CancellationTokenSource.CreateLinkedTokenSource(sessionToken);
        CancellationToken ioToken = _ioCts.Token;

        EventHandler<GenericSingleEventArgs<ReadOnlyMemory<byte>>> onRx = (_, args) =>
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
        };

        _transport.RxReceived += onRx;

        _sendLoopTask = Task.Run(() => RunSendLoopAsync(onRx, ioToken), ioToken);
        _keepaliveTask = Task.Run(() => RunKeepaliveLoopAsync(ioToken), ioToken);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Send loop must not crash the host on transport faults; Log Warn and let the reconnect cycle take over (README §\"Exception Handling\").")]
    private async Task RunSendLoopAsync(EventHandler<GenericSingleEventArgs<ReadOnlyMemory<byte>>> rxHandler, CancellationToken cancellationToken)
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
            _transport.RxReceived -= rxHandler;
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
