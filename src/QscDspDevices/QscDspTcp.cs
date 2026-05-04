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
using QscDspDevices.Connectivity.Redundancy;
using QscDspDevices.LogicTriggers;
using QscDspDevices.Plugin;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.Protocol.JsonRpc;
using QscDspDevices.Transport;

namespace QscDspDevices;

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
    private RoutingCommandQueue? _routingQueue;
    private AudioControlServiceFanout? _fanout;
    private IdGenerator? _ids;
    private string? _primaryHostname;
    private int _primaryPort;
    private ChangeGroupManager? _primaryGroupManager;
    private string? _backupHostname;
    private int _backupPort;
    private RedundantConnectionPair? _redundantPair;
    private bool _disposed;

    // ECP backend (M-ECP). Exactly one of {QRC fields above, ECP fields
    // here} is wired during Initialize, picked by the well-known port
    // (1710 = QRC, 1702 = ECP).
    private bool _useEcp;
    private QscDspDevices.Connectivity.Ecp.EcpConnectionManager? _ecpConnection;
    private QscDspDevices.Connectivity.Ecp.EcpCommandQueue? _ecpQueue;
    private QscDspDevices.AudioControl.Ecp.EcpAudioControlService? _ecpAudio;
    private QscDspDevices.AudioControl.Ecp.EcpAudioRoutingService? _ecpRouting;
    private QscDspDevices.AudioControl.Ecp.EcpAudioZoneEnableService? _ecpZones;
    private QscDspDevices.LogicTriggers.Ecp.EcpLogicTriggerService? _ecpTriggers;

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

    /// <inheritdoc />
    public event EventHandler<GenericSingleEventArgs<string>>? RedundancyStateChanged;

    /// <inheritdoc />
    public event EventHandler<GenericSingleEventArgs<string>>? BackupDeviceConnectionChanged;

    /// <summary>
    /// Gets the runtime guard that enforces the README §4 hard cap of
    /// 3 plugin-owned threads. Test code uses this to assert
    /// <see cref="ThreadCensus.AliveCount"/> in steady state and after
    /// <see cref="Disconnect"/>.
    /// </summary>
    public ThreadCensus ThreadCensus => _threadCensus;

    /// <inheritdoc />
    /// <remarks>
    /// In single-Core mode (no <see cref="SetBackupDeviceConnection"/>),
    /// this returns <see cref="BaseDevice.IsOnline"/> — the only Core
    /// the plugin knows about IS the primary, and it's "active" iff
    /// the connection is up. In redundant mode, this delegates to the
    /// pair's currently-active slot.
    /// </remarks>
    public bool PrimaryDeviceActive => _redundantPair?.PrimaryDeviceActive ?? IsOnline;

    /// <inheritdoc />
    public bool BackupDeviceActive => _redundantPair?.BackupDeviceActive ?? false;

    /// <inheritdoc />
    public bool BackupDeviceOnline => _redundantPair?.BackupDeviceOnline ?? false;

    /// <inheritdoc />
    public bool BackupDeviceExists => _backupHostname is not null;

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

        // Protocol selection by well-known port per add-ecp-protocol §3:
        // 1710 = QRC, 1702 = ECP. Other ports default to QRC with a
        // Logger.Notice. Stash the decision before the rest of the
        // wiring so the ECP branch below can short-circuit.
        if (port == 1702)
        {
            _useEcp = true;
            InitializeEcp(hostId, hostname, port, username, password);
            IsInitialized = true;
            Log.Notice(Id, $"Initialized ECP backend for {hostname}:{port} (coreId={coreId}).");
            return;
        }

        if (port != 1710)
        {
            Log.Notice(hostId, $"Non-standard port {port}; assuming QRC. Use port 1702 to select ECP.");
        }

        // Stash credentials behind a lock so the LogonAction's callback
        // sees fresh values across reconnects without racing a concurrent
        // Initialize.
        lock (_credentialsLock)
        {
            _credentials = new LogonCredentials(username, password);
        }

        // The M3-M5 service tier always enqueues to a RoutingCommandQueue
        // facade. In single-Core mode the facade points at the only
        // queue (the primary's). In redundant mode, the pair coordinator
        // re-points the facade on every active-slot transition.
        var ids = new IdGenerator();
        var scaler = new LevelScaler(hostId);
        var routingQueue = new RoutingCommandQueue(hostId);

        var audioService = new AudioControlService(hostId, _registry, scaler, routingQueue, ids);
        var presetService = new PresetService(hostId, _registry, routingQueue, ids);
        var routingService = new AudioRoutingService(hostId, _registry, routingQueue, ids);
        var zoneService = new AudioZoneEnableService(hostId, _zoneRegistry, routingQueue, ids);
        var triggerService = new LogicTriggerService(hostId, _triggerRegistry, routingQueue, ids);
        var fanout = new AudioControlServiceFanout(
            _registry, _zoneRegistry, _triggerRegistry, routingService, zoneService, triggerService, audioService);

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

        // Build the primary connection's manager. Each connection gets
        // its own queue / dispatcher / group manager / post-connect
        // chain — the facade above abstracts the queue selection on
        // the producer side.
        ConnectionResources primary = BuildConnectionResources(hostId, hostname, port, ids, fanout);

        primary.Manager.StateChanged += OnStateChanged;

        _transport = primary.Transport;
        _queue = primary.Queue;
        _dispatcher = primary.Dispatcher;
        _connectionManager = primary.Manager;
        _audioService = audioService;
        _presetService = presetService;
        _routingService = routingService;
        _zoneEnableService = zoneService;
        _triggerService = triggerService;
        _routingQueue = routingQueue;
        _fanout = fanout;
        _ids = ids;
        _primaryHostname = hostname;
        _primaryPort = port;
        _primaryGroupManager = primary.GroupManager;

        // Single-Core mode: route directly to the primary's queue
        // immediately. The pair coordinator will swap this if/when
        // the backup goes active. The fanout is also wired now (the
        // pair will re-wire it on active swap, but in single-Core
        // mode the wire stays put forever).
        primary.GroupManager.SetDeltaCallback(fanout.Dispatch);
        routingQueue.SetActive(primary.Queue);

        IsInitialized = true;

        Log.Notice(Id, $"Initialized for {hostname}:{port} (coreId={coreId}).");
    }

    /// <inheritdoc />
    public override void Connect()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_useEcp)
        {
            if (_ecpConnection is null)
            {
                Log.Error(string.IsNullOrEmpty(Id) ? "QscDspTcp" : Id, "Connect() called before Initialize().");
                return;
            }

            _ecpConnection.Connect();
            return;
        }

        if (_connectionManager is null)
        {
            string deviceId = string.IsNullOrEmpty(Id) ? "QscDspTcp" : Id;
            Log.Error(deviceId, "Connect() called before Initialize().");
            return;
        }

        // M6: if a backup was configured via SetBackupDeviceConnection
        // and we haven't built the pair yet, lazy-build it now and
        // delegate Connect to the pair (which starts both managers).
        // The single-Core path is unchanged.
        if (_backupHostname is not null && _redundantPair is null)
        {
            BuildRedundantPair();
        }

        if (_redundantPair is not null)
        {
            _redundantPair.Connect();
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

        if (_useEcp)
        {
            _ecpConnection?.Disconnect();
            return;
        }

        if (_redundantPair is not null)
        {
            _redundantPair.Disconnect();
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
        if (_useEcp)
        {
            _ecpAudio?.SetLevel(id, level);
            return;
        }

        _audioService?.SetLevel(id, level);
    }

    /// <inheritdoc />
    public int GetAudioInputLevel(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(GetAudioInputLevel), nameof(id));
        return _useEcp ? (_ecpAudio?.GetLevel(id) ?? 0) : (_audioService?.GetLevel(id) ?? 0);
    }

    /// <inheritdoc />
    public void SetAudioInputMute(string id, bool mute)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(SetAudioInputMute), nameof(id));
        if (_useEcp)
        {
            _ecpAudio?.SetMute(id, mute);
            return;
        }

        _audioService?.SetMute(id, mute);
    }

    /// <inheritdoc />
    public bool GetAudioInputMute(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(GetAudioInputMute), nameof(id));
        return _useEcp ? (_ecpAudio?.GetMute(id) ?? false) : (_audioService?.GetMute(id) ?? false);
    }

    /// <inheritdoc />
    public void SetAudioOutputLevel(string id, int level)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(SetAudioOutputLevel), nameof(id));
        if (_useEcp)
        {
            _ecpAudio?.SetLevel(id, level);
            return;
        }

        _audioService?.SetLevel(id, level);
    }

    /// <inheritdoc />
    public int GetAudioOutputLevel(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(GetAudioOutputLevel), nameof(id));
        return _useEcp ? (_ecpAudio?.GetLevel(id) ?? 0) : (_audioService?.GetLevel(id) ?? 0);
    }

    /// <inheritdoc />
    public void SetAudioOutputMute(string id, bool mute)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(SetAudioOutputMute), nameof(id));
        if (_useEcp)
        {
            _ecpAudio?.SetMute(id, mute);
            return;
        }

        _audioService?.SetMute(id, mute);
    }

    /// <inheritdoc />
    public bool GetAudioOutputMute(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(GetAudioOutputMute), nameof(id));
        return _useEcp ? (_ecpAudio?.GetMute(id) ?? false) : (_audioService?.GetMute(id) ?? false);
    }

    /// <inheritdoc />
    public void RecallAudioPreset(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(RecallAudioPreset), nameof(id));
        if (_useEcp)
        {
            _ecpAudio?.RecallPreset(id);
            return;
        }

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
        if (_useEcp)
        {
            return _ecpRouting?.Query(outputId) ?? string.Empty;
        }

        return _routingService?.GetCurrentSource(outputId) ?? string.Empty;
    }

    /// <inheritdoc />
    public void RouteAudio(string sourceId, string outputId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(sourceId, nameof(RouteAudio), nameof(sourceId));
        ParameterValidator.ThrowIfNullOrEmpty(outputId, nameof(RouteAudio), nameof(outputId));
        if (_useEcp)
        {
            _ecpRouting?.Route(sourceId, outputId);
            return;
        }

        _routingService?.Route(sourceId, outputId);
    }

    /// <inheritdoc />
    public void ClearAudioRoute(string outputId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(outputId, nameof(ClearAudioRoute), nameof(outputId));
        if (_useEcp)
        {
            _ecpRouting?.Clear(outputId);
            return;
        }

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
        if (_useEcp)
        {
            _ecpZones?.Toggle(channelId, zoneId);
            return;
        }

        _zoneEnableService?.Toggle(channelId, zoneId);
    }

    /// <inheritdoc />
    public void SetAudioZoneEnable(string channelId, string zoneId, bool enable)
    {
        ParameterValidator.ThrowIfNullOrEmpty(channelId, nameof(SetAudioZoneEnable), nameof(channelId));
        ParameterValidator.ThrowIfNullOrEmpty(zoneId, nameof(SetAudioZoneEnable), nameof(zoneId));
        if (_useEcp)
        {
            _ecpZones?.Set(channelId, zoneId, enable);
            return;
        }

        _zoneEnableService?.Set(channelId, zoneId, enable);
    }

    /// <inheritdoc />
    public bool QueryAudioZoneEnable(string channelId, string zoneId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(channelId, nameof(QueryAudioZoneEnable), nameof(channelId));
        ParameterValidator.ThrowIfNullOrEmpty(zoneId, nameof(QueryAudioZoneEnable), nameof(zoneId));
        if (_useEcp)
        {
            return _ecpZones?.Query(channelId, zoneId) ?? false;
        }

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
        if (_useEcp)
        {
            _ecpTriggers?.Pulse(id);
            return;
        }

        _triggerService?.Pulse(id);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Per the framework spec, this is called after <c>Initialize</c>
    /// and before <c>Connect</c>. The backup config is stashed; the
    /// pair is constructed in the next <see cref="Connect"/> call.
    /// Re-calling with a new hostname before Connect replaces the
    /// stash; calling after Connect logs <c>Logger.Warn</c> and is a
    /// no-op (a hot reconfiguration would require a Disconnect /
    /// re-Connect cycle).
    /// </remarks>
    public void SetBackupDeviceConnection(string hostname, int port)
    {
        ParameterValidator.ThrowIfNullOrEmpty(hostname, nameof(SetBackupDeviceConnection), nameof(hostname));

        // M-ECP: redundant pairs must use the same protocol on both
        // sides per spec redundancy.md. The primary's protocol is
        // pinned at Initialize() time; refuse any mismatch.
        bool primaryIsEcp = _useEcp;
        bool backupIsEcp = port == 1702;
        if (primaryIsEcp != backupIsEcp)
        {
            string deviceId = string.IsNullOrEmpty(Id) ? "QscDspTcp" : Id;
            Log.Error(deviceId, $"redundant pair must use same protocol on both sides; call refused (primary {(primaryIsEcp ? "ECP" : "QRC")}, backup port {port}).");
            return;
        }

        if (_useEcp)
        {
            // ECP redundancy is implemented via sg-poll on each side
            // (see slice 8); the M6 RedundantConnectionPair coordinator
            // does not yet plug into the EcpConnectionManager. For
            // M-ECP-part-2, refuse with a Logger.Notice and document
            // the gap; the integrator falls back to manual failover.
            Log.Notice(string.IsNullOrEmpty(Id) ? "QscDspTcp" : Id, $"Redundant pair under ECP is not yet wired; backup '{hostname}:{port}' ignored. Tracked as M-ECP-part-3.");
            return;
        }

        if (_redundantPair is not null)
        {
            string deviceId = string.IsNullOrEmpty(Id) ? "QscDspTcp" : Id;
            Log.Warn(deviceId, $"SetBackupDeviceConnection('{hostname}:{port}') called after Connect; ignoring. Re-configure requires Disconnect first.");
            return;
        }

        _backupHostname = hostname;
        _backupPort = port;

        if (!string.IsNullOrEmpty(Id))
        {
            Log.Notice(Id, $"Backup device configured: {hostname}:{port}. Pair will be constructed on next Connect.");
        }
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
            // Disposing the pair also disposes both underlying managers
            // (primary AND backup), so when the pair is present we
            // skip the bare _connectionManager.Dispose() to avoid a
            // double-dispose on the primary.
            if (_redundantPair is not null)
            {
                if (_connectionManager is not null)
                {
                    _connectionManager.StateChanged -= OnStateChanged;
                }

                _redundantPair.Dispose();
            }
            else if (_connectionManager is not null)
            {
                _connectionManager.StateChanged -= OnStateChanged;
                _connectionManager.Dispose();
            }

            _transport?.Dispose();
            _queue?.Dispose();
            _routingQueue?.Dispose();

            if (_ecpConnection is not null)
            {
                _ecpConnection.StateChanged -= OnEcpStateChanged;
                _ecpConnection.Dispose();
            }

            _ecpQueue?.Dispose();
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "The ECP transport, queue, and ConnectionManager are stashed in long-lived fields and disposed in Dispose. CA2000 does not see the ownership transfer through field assignment.")]
    private void InitializeEcp(string hostId, string hostname, int port, string username, string password)
    {
        var transport = BuildTransport(hostname, port);
        var queue = new QscDspDevices.Connectivity.Ecp.EcpCommandQueue(hostId);
        var dispatcher = new QscDspDevices.Protocol.Ecp.EcpDispatcher(hostId);
        var creds = string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password)
            ? null
            : new QscDspDevices.Connectivity.Ecp.EcpCredentials(username ?? string.Empty, password ?? string.Empty);

        var connection = new QscDspDevices.Connectivity.Ecp.EcpConnectionManager(
            hostId,
            transport,
            new ReconnectStrategy(_clock),
            queue,
            dispatcher,
            credentialsSource: () => creds,
            threadCensus: _threadCensus);

        connection.StateChanged += OnEcpStateChanged;

        var scaler = new LevelScaler(hostId);
        var audio = new QscDspDevices.AudioControl.Ecp.EcpAudioControlService(hostId, _registry, scaler, queue);
        var routing = new QscDspDevices.AudioControl.Ecp.EcpAudioRoutingService(hostId, _registry, queue);
        var zones = new QscDspDevices.AudioControl.Ecp.EcpAudioZoneEnableService(hostId, _zoneRegistry, queue);
        var triggers = new QscDspDevices.LogicTriggers.Ecp.EcpLogicTriggerService(hostId, _triggerRegistry, queue);

        audio.AudioLevelChanged += (_, args) =>
        {
            AudioOutputLevelChanged?.Invoke(this, args);
            AudioInputLevelChanged?.Invoke(this, args);
        };
        audio.AudioMuteChanged += (_, args) =>
        {
            AudioOutputMuteChanged?.Invoke(this, args);
            AudioInputMuteChanged?.Invoke(this, args);
        };
        routing.RouteChanged += (_, args) => AudioRouteChanged?.Invoke(this, args);
        zones.ZoneEnableChanged += (_, args) => AudioZoneEnableChanged?.Invoke(this, args);

        _transport = transport;
        _ecpConnection = connection;
        _ecpQueue = queue;
        _ecpAudio = audio;
        _ecpRouting = routing;
        _ecpZones = zones;
        _ecpTriggers = triggers;
        _primaryHostname = hostname;
        _primaryPort = port;
    }

    private void OnEcpStateChanged(object? sender, gcu_common_utils.GenericEventArgs.GenericSingleEventArgs<ConnectionState> args)
    {
        switch (args.Arg)
        {
            case ConnectionState.Connected:
                IsOnline = true;
                NotifyOnlineStatus();
                break;
            case ConnectionState.Disconnecting:
            case ConnectionState.Disconnected:
                IsOnline = false;
                NotifyOnlineStatus();
                break;
            case ConnectionState.Connecting:
                break;
        }
    }

    /// <summary>
    /// Builds the per-connection plumbing — transport, queue, dispatcher,
    /// change-group manager, post-connect chain, and connection manager.
    /// Used once for the primary at <c>Initialize</c> time, and once
    /// more for the backup at <c>Connect</c> time when
    /// <c>SetBackupDeviceConnection</c> has been called.
    /// </summary>
    /// <param name="hostId">The owning device id.</param>
    /// <param name="hostname">The remote hostname.</param>
    /// <param name="port">The remote port.</param>
    /// <param name="ids">Shared id generator.</param>
    /// <param name="fanout">Shared fanout dispatcher.</param>
    /// <returns>The connection's resources.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Returned ConnectionResources owns the manager + transport + queue; the caller (Initialize for primary, BuildRedundantPair for backup) installs them into long-lived fields and disposes them in Dispose / via the pair.")]
    private ConnectionResources BuildConnectionResources(string hostId, string hostname, int port, IdGenerator ids, AudioControlServiceFanout fanout)
    {
        IConnectionTransport transport = BuildTransport(hostname, port);
        var queue = new CommandQueue(hostId);
        var dispatcher = new JsonRpcDispatcher(hostId);
        var groupManager = new ChangeGroupManager(hostId, ids);

        // Each connection has its own Logon + Hydrate chain bound to
        // its own queue + dispatcher. The shared fanout is wired into
        // each group manager separately; in single-Core mode the
        // wiring is permanent, in redundant mode the pair coordinator
        // de-routes the inactive side and re-routes the active.
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

        // Note that fanout is referenced but not yet attached to this
        // group manager — single-Core attaches in Initialize, redundant
        // attaches via the pair coordinator. fanout is captured to keep
        // the parameter from going unused in the single-Core path.
        _ = fanout;

        return new ConnectionResources(transport, queue, dispatcher, groupManager, manager);
    }

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

            case ConnectionState.Disconnecting:
                IsOnline = false;
                NotifyOnlineStatus();

                // Best-effort ChangeGroup.Destroy on graceful disconnect.
                // Single-Core path; redundant deployments hook the
                // equivalent on each side inside RedundantConnectionPair.
                if (_redundantPair is null)
                {
                    DisconnectCleanup.TryEnqueueDestroy(Id, _primaryGroupManager, _queue, _transport);
                }

                break;

            case ConnectionState.Disconnected:
                IsOnline = false;
                NotifyOnlineStatus();
                break;

            case ConnectionState.Connecting:
                // Still offline; do not flip IsOnline yet.
                break;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "BuildRedundantPair takes ownership of the new ConnectionManager via the RedundantConnectionPair, which disposes it on Pair.Dispose. The local goes out of scope after the pair captures the reference; CA2000 cannot see the ownership transfer.")]
    private void BuildRedundantPair()
    {
        if (_ids is null || _fanout is null || _routingQueue is null
            || _connectionManager is null || _queue is null || _dispatcher is null
            || _primaryGroupManager is null || _backupHostname is null)
        {
            return;
        }

        ConnectionResources backup = BuildConnectionResources(Id, _backupHostname, _backupPort, _ids, _fanout);

        var pair = new RedundantConnectionPair(
            Id,
            _connectionManager,
            _queue,
            _primaryGroupManager,
            backup.Manager,
            backup.Queue,
            backup.GroupManager,
            _routingQueue,
            _fanout,
            SwitchbackPolicy.Default,
            backup.Transport,
            _transport);

        // Forward the pair's events to the QscDspTcp surface.
        pair.RedundancyStateChanged += (_, args) => RedundancyStateChanged?.Invoke(this, args);
        pair.BackupDeviceConnectionChanged += (_, args) => BackupDeviceConnectionChanged?.Invoke(this, args);

        _redundantPair = pair;
        Log.Notice(Id, $"Redundant pair built: primary={_primaryHostname}:{_primaryPort}, backup={_backupHostname}:{_backupPort}.");
    }

    private sealed record ConnectionResources(
        IConnectionTransport Transport,
        CommandQueue Queue,
        JsonRpcDispatcher Dispatcher,
        ChangeGroupManager GroupManager,
        ConnectionManager Manager);
}
