// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using gcu_common_utils.GenericEventArgs;
using QscDspDevices.Connectivity.Redundancy;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol;

namespace QscDspDevices.Connectivity.Ecp;

/// <summary>
/// ECP-side redundant pair coordinator. Mirrors the M6
/// <see cref="RedundantConnectionPair"/> shape: tracks per-side
/// <see cref="EngineState"/> from <see cref="EcpEngineStateProbe"/>,
/// runs the M6 <see cref="SwitchbackPolicy"/>, and re-points an
/// <see cref="EcpRoutingCommandQueue"/> on every active-slot
/// transition.
/// </summary>
internal sealed class EcpRedundantConnectionPair : IDisposable
{
    private readonly string _deviceId;
    private readonly EcpConnectionManager _primary;
    private readonly EcpConnectionManager _backup;
    private readonly EcpRoutingCommandQueue _routingQueue;
    private readonly SwitchbackPolicy _policy;
    private readonly EcpEngineStateProbe _primaryProbe;
    private readonly EcpEngineStateProbe _backupProbe;

    private readonly object _stateLock = new();
    private EngineState _primaryState = EngineState.Unknown;
    private EngineState _backupState = EngineState.Unknown;
    private CoreSlot? _activeSlot;
    private ConnectionState _backupConnectionState = ConnectionState.Disconnected;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpRedundantConnectionPair"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="primary">The primary ECP connection manager.</param>
    /// <param name="backup">The backup ECP connection manager.</param>
    /// <param name="routingQueue">The routing facade rewired on every active-slot swap.</param>
    /// <param name="policy">The switchback policy.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public EcpRedundantConnectionPair(
        string deviceId,
        EcpConnectionManager primary,
        EcpConnectionManager backup,
        EcpRoutingCommandQueue routingQueue,
        SwitchbackPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(routingQueue);
        ArgumentNullException.ThrowIfNull(policy);

        _deviceId = deviceId;
        _primary = primary;
        _backup = backup;
        _routingQueue = routingQueue;
        _policy = policy;

        _primaryProbe = new EcpEngineStateProbe(deviceId, primary.Dispatcher, primary.Queue, s => OnEngineState(CoreSlot.Primary, s));
        _backupProbe = new EcpEngineStateProbe(deviceId, backup.Dispatcher, backup.Queue, s => OnEngineState(CoreSlot.Backup, s));

        _backup.StateChanged += OnBackupStateChanged;
    }

    /// <summary>Raised when the active slot changes.</summary>
    public event EventHandler<GenericSingleEventArgs<string>>? RedundancyStateChanged;

    /// <summary>Raised on backup TCP up/down.</summary>
    public event EventHandler<GenericSingleEventArgs<string>>? BackupDeviceConnectionChanged;

    /// <summary>Gets the currently-active slot, or null if neither side is Active.</summary>
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

    /// <summary>Gets a value indicating whether the primary is the active routing target.</summary>
    public bool PrimaryDeviceActive => ActiveSlot == CoreSlot.Primary;

    /// <summary>Gets a value indicating whether the backup is the active routing target.</summary>
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

    /// <summary>Connects both managers in parallel and starts the engine-state probes.</summary>
    public void Connect()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _primary.Connect();
        _backup.Connect();
        _primaryProbe.Start();
        _backupProbe.Start();
    }

    /// <summary>Disconnects both managers and clears routing.</summary>
    public void Disconnect()
    {
        if (_disposed)
        {
            return;
        }

        _primary.Disconnect();
        _backup.Disconnect();

        lock (_stateLock)
        {
            _activeSlot = null;
            _primaryState = EngineState.Unknown;
            _backupState = EngineState.Unknown;
        }

        _routingQueue.SetActive(null);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _backup.StateChanged -= OnBackupStateChanged;
        _primaryProbe.Dispose();
        _backupProbe.Dispose();

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

        ApplyActiveSwitch(newActive);
    }

    private void ApplyActiveSwitch(CoreSlot? newActive)
    {
        EcpCommandQueue? newQueue = newActive switch
        {
            CoreSlot.Primary => _primary.Queue,
            CoreSlot.Backup => _backup.Queue,
            _ => null,
        };

        _routingQueue.SetActive(newQueue);

        if (newActive is null)
        {
            Log.Warn(_deviceId, "ECP redundant pair: no Core is currently Active.");
        }
        else
        {
            Log.Notice(_deviceId, $"ECP redundant pair: active slot is now {newActive}.");
        }

        RedundancyStateChanged?.Invoke(this, new GenericSingleEventArgs<string>(_deviceId));
    }

    private void OnBackupStateChanged(object? sender, GenericSingleEventArgs<ConnectionState> args)
    {
        ConnectionState newState = args.Arg;
        bool fire;
        lock (_stateLock)
        {
            ConnectionState prior = _backupConnectionState;
            _backupConnectionState = newState;
            bool wasConnected = prior == ConnectionState.Connected;
            bool isConnected = newState == ConnectionState.Connected;
            fire = wasConnected != isConnected;
            if (!isConnected && _activeSlot == CoreSlot.Backup)
            {
                _backupState = EngineState.Unknown;
            }
        }

        if (fire)
        {
            BackupDeviceConnectionChanged?.Invoke(this, new GenericSingleEventArgs<string>(_deviceId));
        }
    }
}
