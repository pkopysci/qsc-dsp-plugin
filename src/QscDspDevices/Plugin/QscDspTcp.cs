// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using gcu_common_utils.GenericEventArgs;
using gcu_common_utils.Validation;
using gcu_hardware_service.AudioDevices;
using gcu_hardware_service.BaseDevice;
using gcu_hardware_service.Redundancy;
using gcu_hardware_service.Routable;
using QscDspDevices.AudioControl;
using QscDspDevices.Connectivity;
using QscDspDevices.Connectivity.PostConnect;
using QscDspDevices.LogicTriggers;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.Transport;

namespace QscDspDevices.Plugin;

/// <summary>
/// The plugin's root public class — the one the AV Framework instantiates
/// at runtime. Implements the framework's <see cref="BaseDevice"/> +
/// <see cref="IDsp"/> + <see cref="IAudioRoutable"/> +
/// <see cref="IAudioZoneEnabler"/> + <see cref="IDspLogicTriggerSupport"/>
/// + <see cref="IRedundancySupport"/> contract for QSC Q-SYS Cores.
/// </summary>
/// <remarks>
/// <para>
/// M2 wires the connection lifecycle (Connect/Disconnect/IsOnline/
/// NotifyOnlineStatus) and the queue+dispatcher infrastructure. Audio
/// control, routing, presets, logic triggers, and redundancy land in
/// M3-M6; until then the corresponding interface methods log
/// <c>Logger.Notice</c> "not implemented in M{n}" and return the
/// documented fallback (0/false/empty) without throwing.
/// </para>
/// <para>
/// The README §3 names this class explicitly: "The root public class
/// must be named <c>QscDspTcp</c>." Manufacturer is fixed to
/// <c>"QSC"</c> and Model to <c>"Q-SYS Core"</c> at construction.
/// </para>
/// <para>
/// Public methods are SYNCHRONOUS per README §4 ("The library must be
/// non-blocking and all threading must be managed internally; no public
/// async/await."). Internal threading is owned by the <see cref="ConnectionManager"/>.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "QscDspTcp is the composition root for the plugin and intentionally references the connectivity, transport, protocol, and threading layers it composes.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1859:Use concrete types when possible for improved performance",
    Justification = "Fields are typed against IConnectionTransport / CommandQueue / etc. rather than concrete classes so the test suite can substitute fakes via the seams.")]
public class QscDspTcp : BaseDevice, IDsp, IAudioRoutable, IAudioZoneEnabler, IDspLogicTriggerSupport, IRedundancySupport
{
    private readonly IQrcClock _clock;
    private readonly ThreadCensus _threadCensus;
    private readonly AudioChannelRegistry _registry;
    private readonly AudioZoneRegistry _zoneRegistry;
    private readonly LogicTriggerRegistry _triggerRegistry;
    private readonly object _credentialsLock = new();

    private LogonCredentials _credentials = LogonCredentials.Empty;
    private CommandQueue? _queue;
    private JsonRpcDispatcher? _dispatcher;
    private IConnectionTransport? _transport;
    private ConnectionManager? _connectionManager;
    private AudioControlService? _audioService;
    private PresetService? _presetService;
    private AudioRoutingService? _routingService;
    private AudioZoneEnableService? _zoneEnableService;
    private LogicTriggerService? _triggerService;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="QscDspTcp"/> class
    /// with production wall-clock timing.
    /// </summary>
    public QscDspTcp()
        : this(new SystemClock())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QscDspTcp"/> class
    /// with an explicit clock (used by tests).
    /// </summary>
    /// <param name="clock">The clock implementation.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="clock"/> is null.</exception>
    public QscDspTcp(IQrcClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;

        // Construct the thread census up-front so it is observable even
        // before Initialize() lands a deviceId. Initialize() replaces it
        // with one tagged to the configured deviceId so log messages
        // attribute breaches to the right device.
        _threadCensus = new ThreadCensus("QscDspTcp");

        // The channel registry is also constructed eagerly because
        // Crestron framework hosts may invoke AddInputChannel /
        // AddOutputChannel / AddPreset BEFORE Initialize() runs (the
        // composition order is config-driven, not framework-mandated).
        _registry = new AudioChannelRegistry("QscDspTcp");

        // Same rationale for the M4 zone registry — AddAudioZoneEnable
        // can land before Initialize.
        _zoneRegistry = new AudioZoneRegistry("QscDspTcp");

        // M5 logic-trigger registry — AddDspLogicTrigger can also land
        // pre-Initialize.
        _triggerRegistry = new LogicTriggerRegistry("QscDspTcp");

        Manufacturer = "QSC";
        Model = "Q-SYS Core";
    }

    // M3 raises the four IAudioControl events; M4 raises the routing
    // and zone-enable events; M5 raises DspLogicTriggerStateChanged.
    // The remaining M6 events (redundancy, backup-device-connection)
    // are declared but unraised, so the CS0067 pragma is narrowed to
    // just that pair. M7 retires the suppression entirely.

    /// <inheritdoc />
    public event EventHandler<GenericDualEventArgs<string, string>>? AudioInputLevelChanged;

    /// <inheritdoc />
    public event EventHandler<GenericDualEventArgs<string, string>>? AudioInputMuteChanged;

    /// <inheritdoc />
    public event EventHandler<GenericDualEventArgs<string, string>>? AudioOutputLevelChanged;

    /// <inheritdoc />
    public event EventHandler<GenericDualEventArgs<string, string>>? AudioOutputMuteChanged;

    /// <inheritdoc />
    public event EventHandler<GenericDualEventArgs<string, string>>? AudioRouteChanged;

    /// <inheritdoc />
    public event EventHandler<GenericDualEventArgs<string, string>>? AudioZoneEnableChanged;

    /// <inheritdoc />
    public event EventHandler<GenericSingleEventArgs<string>>? DspLogicTriggerStateChanged;

#pragma warning disable CS0067 // Event raised in a later milestone — see the M6 design notes.

    /// <inheritdoc />
    public event EventHandler<GenericSingleEventArgs<string>>? RedundancyStateChanged;

    /// <inheritdoc />
    public event EventHandler<GenericSingleEventArgs<string>>? BackupDeviceConnectionChanged;
#pragma warning restore CS0067

    /// <summary>
    /// Gets the runtime guard that enforces the README §4 hard cap of
    /// 3 plugin-owned threads. Test code uses this to assert
    /// <see cref="ThreadCensus.AliveCount"/> in steady state and after
    /// <see cref="Disconnect"/>.
    /// </summary>
    public ThreadCensus ThreadCensus => _threadCensus;

    /// <inheritdoc />
    public bool PrimaryDeviceActive => true;

    /// <inheritdoc />
    public bool BackupDeviceActive => false;

    /// <inheritdoc />
    public bool BackupDeviceOnline => false;

    /// <inheritdoc />
    public bool BackupDeviceExists => false;

    /// <inheritdoc />
    public void Initialize(string hostId, int coreId, string hostname, int port, string username, string password)
    {
        ParameterValidator.ThrowIfNullOrEmpty(hostId, nameof(Initialize), nameof(hostId));
        ParameterValidator.ThrowIfNullOrEmpty(hostname, nameof(Initialize), nameof(hostname));

        Id = hostId;
        Label = hostId;

        // coreId is currently unused (QRC operates on a single Core per
        // connection); it lands when M6 introduces redundancy fan-out.
        _ = coreId;

        // Stash credentials behind a lock so the LogonAction's callback
        // sees fresh values across reconnects without racing a concurrent
        // Initialize.
        lock (_credentialsLock)
        {
            _credentials = new LogonCredentials(username, password);
        }

        IConnectionTransport transport = BuildTransport(hostname, port);
        var queue = new CommandQueue(hostId);
        var dispatcher = new JsonRpcDispatcher(hostId);
        var ids = new IdGenerator();
        var scaler = new LevelScaler(hostId);
        var groupManager = new ChangeGroupManager(hostId, ids);
        var audioService = new AudioControlService(hostId, _registry, scaler, queue, ids);
        var presetService = new PresetService(hostId, _registry, queue, ids);
        var routingService = new AudioRoutingService(hostId, _registry, queue, ids);
        var zoneService = new AudioZoneEnableService(hostId, _zoneRegistry, queue, ids);
        var triggerService = new LogicTriggerService(hostId, _triggerRegistry, queue, ids);
        var fanout = new AudioControlServiceFanout(
            _registry, _zoneRegistry, _triggerRegistry, routingService, zoneService, triggerService, audioService);

        groupManager.SetDeltaCallback(fanout.Dispatch);

        // Forward the M3 AudioControlService events to QscDspTcp's own
        // surface — the framework calls IAudioControl.* events on the
        // public class, not the inner service.
        audioService.AudioInputLevelChanged += (_, args) => AudioInputLevelChanged?.Invoke(this, args);
        audioService.AudioInputMuteChanged += (_, args) => AudioInputMuteChanged?.Invoke(this, args);
        audioService.AudioOutputLevelChanged += (_, args) => AudioOutputLevelChanged?.Invoke(this, args);
        audioService.AudioOutputMuteChanged += (_, args) => AudioOutputMuteChanged?.Invoke(this, args);

        // M4: forward the routing + zone-enable events likewise.
        routingService.RouteChanged += (_, args) => AudioRouteChanged?.Invoke(this, args);
        zoneService.ZoneEnableChanged += (_, args) => AudioZoneEnableChanged?.Invoke(this, args);

        // M5: forward the logic-trigger event.
        triggerService.LogicTriggerStateChanged += (_, args) => DspLogicTriggerStateChanged?.Invoke(this, args);

        var logon = new LogonAction(
            hostId,
            () =>
            {
                lock (_credentialsLock)
                {
                    return _credentials;
                }
            },
            queue,
            dispatcher,
            ids);

        var hydrate = new HydrateChangeGroupAction(
            hostId, _registry, _zoneRegistry, _triggerRegistry, groupManager, queue, dispatcher, logon);
        var postConnect = new CompositePostConnectAction(new IPostConnectAction[] { logon, hydrate });

        var manager = new ConnectionManager(
            hostId,
            transport,
            new ReconnectStrategy(_clock),
            queue,
            dispatcher,
            postConnect: postConnect,
            threadCensus: _threadCensus,
            clock: _clock,
            ids: ids);

        manager.StateChanged += OnStateChanged;

        _transport = transport;
        _queue = queue;
        _dispatcher = dispatcher;
        _connectionManager = manager;
        _audioService = audioService;
        _presetService = presetService;
        _routingService = routingService;
        _zoneEnableService = zoneService;
        _triggerService = triggerService;
        IsInitialized = true;

        Log.Notice(Id, $"Initialized for {hostname}:{port} (coreId={coreId}).");
    }

    /// <inheritdoc />
    public override void Connect()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_connectionManager is null)
        {
            string deviceId = string.IsNullOrEmpty(Id) ? "QscDspTcp" : Id;
            Log.Error(deviceId, "Connect() called before Initialize().");
            return;
        }

        _connectionManager.Connect();
    }

    /// <inheritdoc />
    public override void Disconnect()
    {
        if (_disposed)
        {
            return;
        }

        _connectionManager?.Disconnect();
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAudioPresetIds() => _registry.GetPresetIds();

    /// <inheritdoc />
    public IEnumerable<string> GetAudioInputIds() => _registry.GetInputIds();

    /// <inheritdoc />
    public IEnumerable<string> GetAudioOutputIds() => _registry.GetOutputIds();

    /// <inheritdoc />
    public void SetAudioInputLevel(string id, int level)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(SetAudioInputLevel), nameof(id));
        _audioService?.SetLevel(id, level);
    }

    /// <inheritdoc />
    public int GetAudioInputLevel(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(GetAudioInputLevel), nameof(id));
        return _audioService?.GetLevel(id) ?? 0;
    }

    /// <inheritdoc />
    public void SetAudioInputMute(string id, bool mute)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(SetAudioInputMute), nameof(id));
        _audioService?.SetMute(id, mute);
    }

    /// <inheritdoc />
    public bool GetAudioInputMute(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(GetAudioInputMute), nameof(id));
        return _audioService?.GetMute(id) ?? false;
    }

    /// <inheritdoc />
    public void SetAudioOutputLevel(string id, int level)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(SetAudioOutputLevel), nameof(id));
        _audioService?.SetLevel(id, level);
    }

    /// <inheritdoc />
    public int GetAudioOutputLevel(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(GetAudioOutputLevel), nameof(id));
        return _audioService?.GetLevel(id) ?? 0;
    }

    /// <inheritdoc />
    public void SetAudioOutputMute(string id, bool mute)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(SetAudioOutputMute), nameof(id));
        _audioService?.SetMute(id, mute);
    }

    /// <inheritdoc />
    public bool GetAudioOutputMute(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(GetAudioOutputMute), nameof(id));
        return _audioService?.GetMute(id) ?? false;
    }

    /// <inheritdoc />
    public void RecallAudioPreset(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(RecallAudioPreset), nameof(id));
        if (_presetService is null)
        {
            string deviceId = string.IsNullOrEmpty(Id) ? "QscDspTcp" : Id;
            Log.Error(deviceId, $"RecallAudioPreset('{id}') called before Initialize().");
            return;
        }

        _presetService.Recall(id);
    }

    /// <inheritdoc />
    public void AddInputChannel(string id, string levelTag, string muteTag, int bankIndex, int levelMax, int levelMin, int routerIndex, List<string> tags)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(AddInputChannel), nameof(id));
        ParameterValidator.ThrowIfNullOrEmpty(levelTag, nameof(AddInputChannel), nameof(levelTag));
        ParameterValidator.ThrowIfNullOrEmpty(muteTag, nameof(AddInputChannel), nameof(muteTag));

        IReadOnlyList<string> tagList = tags?.ToArray() ?? (IReadOnlyList<string>)Array.Empty<string>();
        _registry.RegisterInput(new AudioChannel(
            id, levelTag, muteTag, levelMin, levelMax, true, routerIndex, bankIndex, tagList));
    }

    /// <inheritdoc />
    public void AddOutputChannel(string id, string levelTag, string muteTag, string routerTag, int routerIndex, int bankIndex, int levelMax, int levelMin, List<string> tags)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(AddOutputChannel), nameof(id));
        ParameterValidator.ThrowIfNullOrEmpty(levelTag, nameof(AddOutputChannel), nameof(levelTag));
        ParameterValidator.ThrowIfNullOrEmpty(muteTag, nameof(AddOutputChannel), nameof(muteTag));

        IReadOnlyList<string> tagList = tags?.ToArray() ?? (IReadOnlyList<string>)Array.Empty<string>();
        _registry.RegisterOutput(new AudioChannel(
            id, levelTag, muteTag, levelMin, levelMax, false, routerIndex, bankIndex, tagList, routerTag ?? string.Empty));
    }

    /// <inheritdoc />
    public void AddPreset(string id, string bank, int index)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(AddPreset), nameof(id));
        ParameterValidator.ThrowIfNullOrEmpty(bank, nameof(AddPreset), nameof(bank));
        _registry.RegisterPreset(new AudioPreset(id, bank, index));
    }

    /// <inheritdoc />
    public string GetCurrentAudioSource(string outputId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(outputId, nameof(GetCurrentAudioSource), nameof(outputId));
        return _routingService?.GetCurrentSource(outputId) ?? string.Empty;
    }

    /// <inheritdoc />
    public void RouteAudio(string sourceId, string outputId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(sourceId, nameof(RouteAudio), nameof(sourceId));
        ParameterValidator.ThrowIfNullOrEmpty(outputId, nameof(RouteAudio), nameof(outputId));
        _routingService?.Route(sourceId, outputId);
    }

    /// <inheritdoc />
    public void ClearAudioRoute(string outputId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(outputId, nameof(ClearAudioRoute), nameof(outputId));
        _routingService?.Clear(outputId);
    }

    /// <inheritdoc />
    public void AddAudioZoneEnable(string channelId, string zoneId, string controlTag)
    {
        ParameterValidator.ThrowIfNullOrEmpty(channelId, nameof(AddAudioZoneEnable), nameof(channelId));
        ParameterValidator.ThrowIfNullOrEmpty(zoneId, nameof(AddAudioZoneEnable), nameof(zoneId));
        ParameterValidator.ThrowIfNullOrEmpty(controlTag, nameof(AddAudioZoneEnable), nameof(controlTag));
        _zoneRegistry.TryRegister(channelId, zoneId, controlTag);
    }

    /// <inheritdoc />
    public void RemoveAudioZoneEnable(string channelId, string zoneId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(channelId, nameof(RemoveAudioZoneEnable), nameof(channelId));
        ParameterValidator.ThrowIfNullOrEmpty(zoneId, nameof(RemoveAudioZoneEnable), nameof(zoneId));
        _zoneRegistry.Remove(channelId, zoneId);
    }

    /// <inheritdoc />
    public void ToggleAudioZoneEnable(string channelId, string zoneId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(channelId, nameof(ToggleAudioZoneEnable), nameof(channelId));
        ParameterValidator.ThrowIfNullOrEmpty(zoneId, nameof(ToggleAudioZoneEnable), nameof(zoneId));
        _zoneEnableService?.Toggle(channelId, zoneId);
    }

    /// <inheritdoc />
    public void SetAudioZoneEnable(string channelId, string zoneId, bool enable)
    {
        ParameterValidator.ThrowIfNullOrEmpty(channelId, nameof(SetAudioZoneEnable), nameof(channelId));
        ParameterValidator.ThrowIfNullOrEmpty(zoneId, nameof(SetAudioZoneEnable), nameof(zoneId));
        _zoneEnableService?.Set(channelId, zoneId, enable);
    }

    /// <inheritdoc />
    public bool QueryAudioZoneEnable(string channelId, string zoneId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(channelId, nameof(QueryAudioZoneEnable), nameof(channelId));
        ParameterValidator.ThrowIfNullOrEmpty(zoneId, nameof(QueryAudioZoneEnable), nameof(zoneId));
        return _zoneEnableService?.Query(channelId, zoneId) ?? false;
    }

    /// <inheritdoc />
    public void AddDspLogicTrigger(string id, string tagName, List<string> tags)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(AddDspLogicTrigger), nameof(id));
        ParameterValidator.ThrowIfNullOrEmpty(tagName, nameof(AddDspLogicTrigger), nameof(tagName));
        _ = tags; // The tags parameter is informational; not used by the QRC mapping.
        _triggerRegistry.Register(id, tagName);
    }

    /// <inheritdoc />
    public void PulseDspLogicTrigger(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(PulseDspLogicTrigger), nameof(id));
        _triggerService?.Pulse(id);
    }

    /// <inheritdoc />
    public void SetBackupDeviceConnection(string hostname, int port)
    {
        ParameterValidator.ThrowIfNullOrEmpty(hostname, nameof(SetBackupDeviceConnection), nameof(hostname));
        Log.Notice(Id, $"SetBackupDeviceConnection('{hostname}:{port}') — not implemented in M2 (lands in M6).");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Standard <see cref="IDisposable"/> pattern.
    /// </summary>
    /// <param name="disposing">True when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            if (_connectionManager is not null)
            {
                _connectionManager.StateChanged -= OnStateChanged;
                _connectionManager.Dispose();
            }

            _transport?.Dispose();
            _queue?.Dispose();
        }
    }

    /// <summary>
    /// Constructs the transport for the supplied endpoint. Virtual so
    /// tests can subclass <see cref="QscDspTcp"/> and substitute a fake
    /// transport without touching the production
    /// <see cref="BasicTcpClientTransport"/> path (which would invoke
    /// the framework stub's NotImplementedException bodies).
    /// </summary>
    /// <param name="hostname">The remote hostname.</param>
    /// <param name="port">The remote TCP port.</param>
    /// <returns>The transport instance.</returns>
    protected virtual IConnectionTransport BuildTransport(string hostname, int port)
        => new BasicTcpClientTransport(hostname, port);

    /// <summary>
    /// Forwards <see cref="ConnectionManager"/> state changes to the
    /// framework's <see cref="BaseDevice.IsOnline"/> + <see cref="BaseDevice.NotifyOnlineStatus"/>
    /// surface. Per README §3 the IsOnline property MUST be set BEFORE
    /// NotifyOnlineStatus() is called.
    /// </summary>
    private void OnStateChanged(object? sender, GenericSingleEventArgs<ConnectionState> args)
    {
        switch (args.Arg)
        {
            case ConnectionState.Connected:
                IsOnline = true;
                NotifyOnlineStatus();
                break;

            case ConnectionState.Disconnected:
            case ConnectionState.Disconnecting:
                IsOnline = false;
                NotifyOnlineStatus();
                break;

            case ConnectionState.Connecting:
                // Still offline; do not flip IsOnline yet.
                break;
        }
    }
}
