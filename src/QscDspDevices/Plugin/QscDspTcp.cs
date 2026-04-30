// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using gcu_common_utils.GenericEventArgs;
using gcu_common_utils.Validation;
using gcu_hardware_service.AudioDevices;
using gcu_hardware_service.BaseDevice;
using gcu_hardware_service.Redundancy;
using gcu_hardware_service.Routable;
using QscDspDevices.Connectivity;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.Protocol;
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

    private CommandQueue? _queue;
    private JsonRpcDispatcher? _dispatcher;
    private IConnectionTransport? _transport;
    private ConnectionManager? _connectionManager;
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
        Manufacturer = "QSC";
        Model = "Q-SYS Core";
    }

    // M2 declares every event the framework interfaces require but does
    // not raise them — the producers (audio control, routing, presets,
    // logic, redundancy) land in M3-M6. CS0067 fires for events that are
    // declared and never invoked; we suppress narrowly here with the
    // intent documented inline. Each subsequent milestone invokes the
    // events it owns and the suppression naturally becomes unnecessary.
#pragma warning disable CS0067 // Event raised in a later milestone — see the M2 design.md.

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
#pragma warning restore CS0067

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

        // M2 captures these for logging only; M3 wires Logon (username/password)
        // and uses coreId for QSC component-control fan-out.
        _ = coreId;
        _ = username;
        _ = password;

        IConnectionTransport transport = BuildTransport(hostname, port);
        var queue = new CommandQueue(hostId);
        var dispatcher = new JsonRpcDispatcher(hostId);
        var manager = new ConnectionManager(
            hostId,
            transport,
            new ReconnectStrategy(_clock),
            queue,
            dispatcher);

        manager.StateChanged += OnStateChanged;

        _transport = transport;
        _queue = queue;
        _dispatcher = dispatcher;
        _connectionManager = manager;
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
    public IEnumerable<string> GetAudioPresetIds() => Array.Empty<string>();

    /// <inheritdoc />
    public IEnumerable<string> GetAudioInputIds() => Array.Empty<string>();

    /// <inheritdoc />
    public IEnumerable<string> GetAudioOutputIds() => Array.Empty<string>();

    /// <inheritdoc />
    public void SetAudioInputLevel(string id, int level)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(SetAudioInputLevel), nameof(id));
        Log.Notice(Id, $"SetAudioInputLevel('{id}', {level}) — not implemented in M2 (lands in M3).");
    }

    /// <inheritdoc />
    public int GetAudioInputLevel(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(GetAudioInputLevel), nameof(id));
        return 0;
    }

    /// <inheritdoc />
    public void SetAudioInputMute(string id, bool mute)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(SetAudioInputMute), nameof(id));
        Log.Notice(Id, $"SetAudioInputMute('{id}', {mute}) — not implemented in M2 (lands in M3).");
    }

    /// <inheritdoc />
    public bool GetAudioInputMute(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(GetAudioInputMute), nameof(id));
        return false;
    }

    /// <inheritdoc />
    public void SetAudioOutputLevel(string id, int level)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(SetAudioOutputLevel), nameof(id));
        Log.Notice(Id, $"SetAudioOutputLevel('{id}', {level}) — not implemented in M2 (lands in M3).");
    }

    /// <inheritdoc />
    public int GetAudioOutputLevel(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(GetAudioOutputLevel), nameof(id));
        return 0;
    }

    /// <inheritdoc />
    public void SetAudioOutputMute(string id, bool mute)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(SetAudioOutputMute), nameof(id));
        Log.Notice(Id, $"SetAudioOutputMute('{id}', {mute}) — not implemented in M2 (lands in M3).");
    }

    /// <inheritdoc />
    public bool GetAudioOutputMute(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(GetAudioOutputMute), nameof(id));
        return false;
    }

    /// <inheritdoc />
    public void RecallAudioPreset(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(RecallAudioPreset), nameof(id));
        Log.Notice(Id, $"RecallAudioPreset('{id}') — not implemented in M2 (lands in M3).");
    }

    /// <inheritdoc />
    public void AddInputChannel(string id, string levelTag, string muteTag, int bankIndex, int levelMax, int levelMin, int routerIndex, List<string> tags)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(AddInputChannel), nameof(id));
        Log.Notice(Id, $"AddInputChannel('{id}') — not implemented in M2 (lands in M3).");
    }

    /// <inheritdoc />
    public void AddOutputChannel(string id, string levelTag, string muteTag, string routerTag, int routerIndex, int bankIndex, int levelMax, int levelMin, List<string> tags)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(AddOutputChannel), nameof(id));
        Log.Notice(Id, $"AddOutputChannel('{id}') — not implemented in M2 (lands in M3).");
    }

    /// <inheritdoc />
    public void AddPreset(string id, string bank, int index)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(AddPreset), nameof(id));
        Log.Notice(Id, $"AddPreset('{id}') — not implemented in M2 (lands in M3).");
    }

    /// <inheritdoc />
    public string GetCurrentAudioSource(string outputId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(outputId, nameof(GetCurrentAudioSource), nameof(outputId));
        return string.Empty;
    }

    /// <inheritdoc />
    public void RouteAudio(string sourceId, string outputId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(sourceId, nameof(RouteAudio), nameof(sourceId));
        ParameterValidator.ThrowIfNullOrEmpty(outputId, nameof(RouteAudio), nameof(outputId));
        Log.Notice(Id, $"RouteAudio('{sourceId}' -> '{outputId}') — not implemented in M2 (lands in M4).");
    }

    /// <inheritdoc />
    public void ClearAudioRoute(string outputId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(outputId, nameof(ClearAudioRoute), nameof(outputId));
        Log.Notice(Id, $"ClearAudioRoute('{outputId}') — not implemented in M2 (lands in M4).");
    }

    /// <inheritdoc />
    public void AddAudioZoneEnable(string channelId, string zoneId, string controlTag)
    {
        ParameterValidator.ThrowIfNullOrEmpty(channelId, nameof(AddAudioZoneEnable), nameof(channelId));
        ParameterValidator.ThrowIfNullOrEmpty(zoneId, nameof(AddAudioZoneEnable), nameof(zoneId));
        Log.Notice(Id, $"AddAudioZoneEnable('{channelId}','{zoneId}') — not implemented in M2 (lands in M4).");
    }

    /// <inheritdoc />
    public void RemoveAudioZoneEnable(string channelId, string zoneId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(channelId, nameof(RemoveAudioZoneEnable), nameof(channelId));
        ParameterValidator.ThrowIfNullOrEmpty(zoneId, nameof(RemoveAudioZoneEnable), nameof(zoneId));
    }

    /// <inheritdoc />
    public void ToggleAudioZoneEnable(string channelId, string zoneId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(channelId, nameof(ToggleAudioZoneEnable), nameof(channelId));
        ParameterValidator.ThrowIfNullOrEmpty(zoneId, nameof(ToggleAudioZoneEnable), nameof(zoneId));
    }

    /// <inheritdoc />
    public void SetAudioZoneEnable(string channelId, string zoneId, bool enable)
    {
        ParameterValidator.ThrowIfNullOrEmpty(channelId, nameof(SetAudioZoneEnable), nameof(channelId));
        ParameterValidator.ThrowIfNullOrEmpty(zoneId, nameof(SetAudioZoneEnable), nameof(zoneId));
    }

    /// <inheritdoc />
    public bool QueryAudioZoneEnable(string channelId, string zoneId)
    {
        ParameterValidator.ThrowIfNullOrEmpty(channelId, nameof(QueryAudioZoneEnable), nameof(channelId));
        ParameterValidator.ThrowIfNullOrEmpty(zoneId, nameof(QueryAudioZoneEnable), nameof(zoneId));
        return false;
    }

    /// <inheritdoc />
    public void AddDspLogicTrigger(string id, string tagName, List<string> tags)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(AddDspLogicTrigger), nameof(id));
    }

    /// <inheritdoc />
    public void PulseDspLogicTrigger(string id)
    {
        ParameterValidator.ThrowIfNullOrEmpty(id, nameof(PulseDspLogicTrigger), nameof(id));
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
