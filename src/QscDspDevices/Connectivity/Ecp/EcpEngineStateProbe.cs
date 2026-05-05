// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using gcu_common_utils.GenericEventArgs;
using QscDspDevices.Connectivity.Redundancy;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.Ecp;

namespace QscDspDevices.Connectivity.Ecp;

/// <summary>
/// ECP-side equivalent of <c>EngineStatusObserver</c>: schedules an
/// <c>sg</c> probe every 2 s on the connection's command queue, parses
/// the <c>sr</c> reply, and forwards <see cref="EngineState"/>
/// transitions (Active / Standby) to a registered callback. The
/// existing M6 <c>RedundantConnectionPair</c> coordinator consumes
/// the same shape from <see cref="EngineStatusObserver"/>; this probe
/// is the protocol-specific bridge.
/// </summary>
internal sealed class EcpEngineStateProbe : IDisposable
{
    /// <summary>Default poll cadence (2 s).</summary>
    public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(2);

    private readonly string _deviceId;
    private readonly EcpDispatcher _dispatcher;
    private readonly EcpCommandQueue _queue;
    private readonly Action<EngineState> _onStateChanged;
    private readonly TimeSpan _pollInterval;
    private readonly EventHandler<GenericSingleEventArgs<EcpResponse>> _handler;
    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;
    private EngineState _lastState = EngineState.Unknown;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpEngineStateProbe"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="dispatcher">The ECP dispatcher to listen on.</param>
    /// <param name="queue">The ECP command queue used to enqueue <c>sg</c> probes.</param>
    /// <param name="onStateChanged">Callback invoked on every observed state transition.</param>
    /// <param name="pollInterval">Optional poll cadence; defaults to 2 s.</param>
    /// <exception cref="ArgumentNullException">If any required argument is null.</exception>
    public EcpEngineStateProbe(
        string deviceId,
        EcpDispatcher dispatcher,
        EcpCommandQueue queue,
        Action<EngineState> onStateChanged,
        TimeSpan? pollInterval = null)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(onStateChanged);

        _deviceId = deviceId;
        _dispatcher = dispatcher;
        _queue = queue;
        _onStateChanged = onStateChanged;
        _pollInterval = pollInterval ?? DefaultPollInterval;

        _handler = OnResponse;
        _dispatcher.ResponseReceived += _handler;
    }

    /// <summary>
    /// Starts the periodic <c>sg</c> probe. Idempotent — calling
    /// twice is a no-op.
    /// </summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_pollTask is not null)
        {
            return;
        }

        _pollCts = new CancellationTokenSource();
        CancellationToken token = _pollCts.Token;
        _pollTask = Task.Run(() => RunPollLoopAsync(token), CancellationToken.None);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _dispatcher.ResponseReceived -= _handler;

        try
        {
            _pollCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already torn down.
        }

        try
        {
            _pollTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Cancellation during shutdown.
        }

        _pollCts?.Dispose();
    }

    private async Task RunPollLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _queue.TryEnqueue(EcpCommand.StatusGet());

            try
            {
                await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private void OnResponse(object? sender, GenericSingleEventArgs<EcpResponse> args)
    {
        if (args.Arg.Kind != EcpResponseKind.StatusReport)
        {
            return;
        }

        EngineState newState = args.Arg.IsActive == 1 ? EngineState.Active : EngineState.Standby;
        if (newState == _lastState)
        {
            return;
        }

        _lastState = newState;
        Log.Debug(_deviceId, $"ECP engine state -> {newState}");
        _onStateChanged(newState);
    }
}
