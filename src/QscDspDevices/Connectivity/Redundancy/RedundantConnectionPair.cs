// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using gcu_common_utils.GenericEventArgs;
using QscDspDevices.AudioControl;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.Transport;

namespace QscDspDevices.Connectivity.Redundancy;

/// <summary>
/// Owns two <see cref="ConnectionManager"/> instances (primary +
/// backup), tracks their per-slot <see cref="EngineState"/>, and
/// designates one as the routing target for outbound writes via the
/// shared <see cref="RoutingCommandQueue"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each underlying manager keeps its M2-M5 shape unchanged. The
/// pair adds the active-router and the EngineStatus subscriber on
/// top.
/// </para>
/// <para>
/// Switchover triggers re-subscription of the AutoPoll fanout to
/// the newly-active manager's <see cref="ChangeGroupManager"/>; the
/// inactive side stops driving cache updates until it becomes
/// active again. Caches are NOT replicated across the pair — the
/// new active's first AutoPoll cycle reconciles cache with the
/// newly-active Core's actual state.
/// </para>
/// </remarks>
public sealed class RedundantConnectionPair : IDisposable
{
    private readonly string _deviceId;
    private readonly ConnectionManager _primary;
    private readonly ConnectionManager _backup;
    private readonly CommandQueue _primaryQueue;
    private readonly CommandQueue _backupQueue;
    private readonly ChangeGroupManager _primaryGroupManager;
    private readonly ChangeGroupManager _backupGroupManager;
    private readonly IConnectionTransport? _primaryTransport;
    private readonly IConnectionTransport? _backupTransport;
    private readonly RoutingCommandQueue _routingQueue;
    private readonly AudioControlServiceFanout _fanout;
    private readonly SwitchbackPolicy _policy;
    private readonly EngineStatusObserver _primaryObserver;
    private readonly EngineStatusObserver _backupObserver;

    private readonly object _stateLock = new();
    private EngineState _primaryState = EngineState.Unknown;
    private EngineState _backupState = EngineState.Unknown;
    private CoreSlot? _activeSlot;
    private ConnectionState _primaryConnectionState = ConnectionState.Disconnected;
    private ConnectionState _backupConnectionState = ConnectionState.Disconnected;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedundantConnectionPair"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="primary">The primary manager.</param>
    /// <param name="primaryQueue">The primary's command queue.</param>
    /// <param name="primaryGroupManager">The primary's change-group manager.</param>
    /// <param name="backup">The backup manager.</param>
    /// <param name="backupQueue">The backup's command queue.</param>
    /// <param name="backupGroupManager">The backup's change-group manager.</param>
    /// <param name="routingQueue">The shared routing facade the service tier enqueues to.</param>
    /// <param name="fanout">The AutoPoll fanout dispatcher (re-subscribed on switchover).</param>
    /// <param name="policy">The switchback policy.</param>
    /// <param name="backupTransport">The backup transport, owned by the pair and disposed on <see cref="Dispose"/>. May be null in tests that own the stub transport themselves.</param>
    /// <param name="primaryTransport">The primary transport, used only to gate the per-side <c>ChangeGroup.Destroy</c> attempt on graceful disconnect. The lifetime is owned by the caller (typically <see cref="QscDspTcp"/>); the pair does not dispose it.</param>
    /// <exception cref="ArgumentNullException">If any required argument is null.</exception>
    public RedundantConnectionPair(
        string deviceId,
        ConnectionManager primary,
        CommandQueue primaryQueue,
        ChangeGroupManager primaryGroupManager,
        ConnectionManager backup,
        CommandQueue backupQueue,
        ChangeGroupManager backupGroupManager,
        RoutingCommandQueue routingQueue,
        AudioControlServiceFanout fanout,
        SwitchbackPolicy policy,
        IConnectionTransport? backupTransport = null,
        IConnectionTransport? primaryTransport = null)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(primaryQueue);
        ArgumentNullException.ThrowIfNull(primaryGroupManager);
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(backupQueue);
        ArgumentNullException.ThrowIfNull(backupGroupManager);
        ArgumentNullException.ThrowIfNull(routingQueue);
        ArgumentNullException.ThrowIfNull(fanout);
        ArgumentNullException.ThrowIfNull(policy);

        _deviceId = deviceId;
        _primary = primary;
        _primaryQueue = primaryQueue;
        _primaryGroupManager = primaryGroupManager;
        _backup = backup;
        _backupQueue = backupQueue;
        _backupGroupManager = backupGroupManager;
        _routingQueue = routingQueue;
        _fanout = fanout;
        _policy = policy;
        _backupTransport = backupTransport;
        _primaryTransport = primaryTransport;

        _primaryObserver = new EngineStatusObserver(deviceId, primary.Dispatcher, s => OnEngineState(CoreSlot.Primary, s));
        _backupObserver = new EngineStatusObserver(deviceId, backup.Dispatcher, s => OnEngineState(CoreSlot.Backup, s));

        // Track each side's TCP up/down. The primary handler exists so a
        // primary TCP drop while it is the active slot can re-evaluate
        // and fail over — EngineStatus pushes never arrive on a dead
        // socket. The backup handler also fires BackupDeviceConnectionChanged.
        _primary.StateChanged += OnPrimaryStateChanged;
        _backup.StateChanged += OnBackupStateChanged;
    }

    /// <summary>Raised when the active slot changes (failover or switchback).</summary>
    public event EventHandler<GenericSingleEventArgs<string>>? RedundancyStateChanged;

    /// <summary>Raised when the backup connection establishes or loses its TCP socket.</summary>
    public event EventHandler<GenericSingleEventArgs<string>>? BackupDeviceConnectionChanged;

    /// <summary>Gets the primary connection manager (exposed for tests).</summary>
    public ConnectionManager Primary => _primary;

    /// <summary>Gets the backup connection manager (exposed for tests).</summary>
    public ConnectionManager Backup => _backup;

    /// <summary>Gets the currently-active slot, or <c>null</c> if no Core is active.</summary>
    public CoreSlot? ActiveSlot
    {
        get
        {
            lock (_stateLock)
            {
                return _activeSlot;
            }
        }
    }

    /// <summary>Gets a value indicating whether the primary is the currently-active routing target.</summary>
    public bool PrimaryDeviceActive => ActiveSlot == CoreSlot.Primary;

    /// <summary>Gets a value indicating whether the backup is the currently-active routing target.</summary>
    public bool BackupDeviceActive => ActiveSlot == CoreSlot.Backup;

    /// <summary>Gets a value indicating whether the backup's TCP socket is currently up.</summary>
    public bool BackupDeviceOnline
    {
        get
        {
            lock (_stateLock)
            {
                return _backupConnectionState == ConnectionState.Connected;
            }
        }
    }

    /// <summary>Starts both managers; the M2-M5 connect lifecycle runs on each in parallel.</summary>
    public void Connect()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _primary.Connect();
        _backup.Connect();
    }

    /// <summary>Stops both managers and clears the active routing target.</summary>
    public void Disconnect()
    {
        if (_disposed)
        {
            return;
        }

        _primary.Disconnect();
        _backup.Disconnect();

        CoreSlot? oldActive;
        lock (_stateLock)
        {
            oldActive = _activeSlot;
            _activeSlot = null;
            _primaryState = EngineState.Unknown;
            _backupState = EngineState.Unknown;
        }

        if (oldActive is not null)
        {
            ApplyActiveSwitch(oldActive, null);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _primary.StateChanged -= OnPrimaryStateChanged;
        _backup.StateChanged -= OnBackupStateChanged;
        _primaryObserver.Dispose();
        _backupObserver.Dispose();

        try
        {
            _primary.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already torn down.
        }

        try
        {
            _backup.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already torn down.
        }

        // The backup-side resources are owned by the pair: the primary's
        // queue and transport are owned by QscDspTcp (constructed before
        // SetBackupDeviceConnection is called), but the backup's were
        // built by the pair's owner specifically to hand off here.
        _backupQueue.Dispose();
        _backupTransport?.Dispose();
    }

    private void OnEngineState(CoreSlot slot, EngineState state)
    {
        CoreSlot? newActive;
        CoreSlot? oldActive;
        lock (_stateLock)
        {
            if (slot == CoreSlot.Primary)
            {
                _primaryState = state;
            }
            else
            {
                _backupState = state;
            }

            oldActive = _activeSlot;
            newActive = _policy.PickActive(_activeSlot, _primaryState, _backupState);
            _activeSlot = newActive;
        }

        if (newActive == oldActive)
        {
            return;
        }

        ApplyActiveSwitch(oldActive, newActive);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "ChangeGroupManager.SetDeltaCallback is the seam where M3-M5 services hook in; a misbehaving callback re-registration must not crash the host (README §\"Exception Handling\").")]
    private void ApplyActiveSwitch(CoreSlot? oldActive, CoreSlot? newActive)
    {
        // De-route the old active's AutoPoll dispatch first (so the
        // inactive side stops mutating shared caches), then re-route to
        // the new active's group manager and re-point the routing queue.
        if (oldActive == CoreSlot.Primary)
        {
            _primaryGroupManager.SetDeltaCallback(_ => { });
        }
        else if (oldActive == CoreSlot.Backup)
        {
            _backupGroupManager.SetDeltaCallback(_ => { });
        }

        CommandQueue? newQueue = newActive switch
        {
            CoreSlot.Primary => _primaryQueue,
            CoreSlot.Backup => _backupQueue,
            _ => null,
        };

        ChangeGroupManager? newGroupManager = newActive switch
        {
            CoreSlot.Primary => _primaryGroupManager,
            CoreSlot.Backup => _backupGroupManager,
            _ => null,
        };

        try
        {
            newGroupManager?.SetDeltaCallback(_fanout.Dispatch);
        }
        catch (Exception ex)
        {
            Log.Error(_deviceId, $"Failed to re-attach AutoPoll fanout on switchover: {ex.GetType().Name}: {ex.Message}");
        }

        _routingQueue.SetActive(newQueue);

        if (newActive is null)
        {
            Log.Warn(_deviceId, "Redundant pair: no Core is currently Active. Writes will be refused until one returns.");
        }
        else
        {
            Log.Notice(_deviceId, $"Redundant pair: active slot is now {newActive}. Was {oldActive?.ToString() ?? "<none>"}.");
        }

        RedundancyStateChanged?.Invoke(this, new GenericSingleEventArgs<string>(_deviceId));
    }

    private void OnPrimaryStateChanged(object? sender, GenericSingleEventArgs<ConnectionState> args)
    {
        ConnectionState newState = args.Arg;
        if (newState == ConnectionState.Disconnecting)
        {
            DisconnectCleanup.TryEnqueueDestroy(_deviceId, _primaryGroupManager, _primaryQueue, _primaryTransport);
        }

        lock (_stateLock)
        {
            _primaryConnectionState = newState;

            // If the primary just dropped while it was the active, force
            // a re-evaluation under the policy. EngineStatus pushes will
            // never arrive on a dead socket, so the State observer alone
            // cannot detect this; the TCP-state handler is the trigger.
            if (newState != ConnectionState.Connected && _activeSlot == CoreSlot.Primary)
            {
                _primaryState = EngineState.Unknown;
            }
        }

        if (newState != ConnectionState.Connected)
        {
            CoreSlot? newActive;
            CoreSlot? oldActive;
            lock (_stateLock)
            {
                oldActive = _activeSlot;
                newActive = _policy.PickActive(_activeSlot, _primaryState, _backupState);
                _activeSlot = newActive;
            }

            if (newActive != oldActive)
            {
                ApplyActiveSwitch(oldActive, newActive);
            }
        }
    }

    private void OnBackupStateChanged(object? sender, GenericSingleEventArgs<ConnectionState> args)
    {
        ConnectionState newState = args.Arg;
        if (newState == ConnectionState.Disconnecting)
        {
            DisconnectCleanup.TryEnqueueDestroy(_deviceId, _backupGroupManager, _backupQueue, _backupTransport);
        }

        bool fire;
        lock (_stateLock)
        {
            ConnectionState prior = _backupConnectionState;
            _backupConnectionState = newState;

            // Fire on every transition into or out of Connected.
            bool wasConnected = prior == ConnectionState.Connected;
            bool isConnected = newState == ConnectionState.Connected;
            fire = wasConnected != isConnected;

            // If the backup just dropped while it was the active, force a
            // re-evaluation under the policy. The EngineStatusObserver
            // alone cannot detect a TCP drop because no notification
            // arrives; the state-change-handler is the trigger.
            if (!isConnected && _activeSlot == CoreSlot.Backup)
            {
                _backupState = EngineState.Unknown;
            }
        }

        if (fire)
        {
            BackupDeviceConnectionChanged?.Invoke(this, new GenericSingleEventArgs<string>(_deviceId));
        }

        // Re-evaluate active outside the lock (the observer also calls
        // OnEngineState; this path covers TCP drops where no
        // EngineStatus push will arrive).
        if (newState != ConnectionState.Connected)
        {
            CoreSlot? newActive;
            CoreSlot? oldActive;
            lock (_stateLock)
            {
                oldActive = _activeSlot;
                newActive = _policy.PickActive(_activeSlot, _primaryState, _backupState);
                _activeSlot = newActive;
            }

            if (newActive != oldActive)
            {
                ApplyActiveSwitch(oldActive, newActive);
            }
        }
    }
}
