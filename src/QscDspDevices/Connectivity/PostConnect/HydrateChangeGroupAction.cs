// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using QscDspDevices.AudioControl;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.Connectivity.PostConnect;

/// <summary>
/// Post-connect action that builds the QRC change group from the
/// channel registry and turns on AutoPoll. Must run AFTER any
/// <see cref="LogonAction"/> has settled — this action waits on
/// <see cref="LogonAction.WaitForCompletionAsync"/> when supplied
/// so the subscribe never races ahead of the Logon response.
/// </summary>
/// <remarks>
/// <para>
/// On every (re)connect, the action recreates the change group
/// from scratch: one <c>ChangeGroup.AddControl</c> per registered
/// level-tag and mute-tag, then one <c>ChangeGroup.AutoPoll</c>.
/// The <see cref="JsonRpcDispatcher"/> is informed of the AutoPoll
/// id so subsequent server pushes route to the
/// <see cref="ChangeGroupManager"/>.
/// </para>
/// </remarks>
public sealed class HydrateChangeGroupAction : IPostConnectAction
{
    private readonly string _deviceId;
    private readonly AudioChannelRegistry _registry;
    private readonly AudioZoneRegistry? _zoneRegistry;
    private readonly ChangeGroupManager _groupManager;
    private readonly CommandQueue _queue;
    private readonly JsonRpcDispatcher _dispatcher;
    private readonly LogonAction? _logon;

    /// <summary>
    /// Initializes a new instance of the <see cref="HydrateChangeGroupAction"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id, used in log messages.</param>
    /// <param name="registry">The channel registry whose level-tags and mute-tags are subscribed.</param>
    /// <param name="groupManager">The change-group manager (also receives AutoPoll pushes).</param>
    /// <param name="queue">The command queue.</param>
    /// <param name="dispatcher">The JSON-RPC dispatcher (for AutoPoll subscription registration).</param>
    /// <param name="logon">Optional Logon action whose completion this action waits on; null when no Logon is configured.</param>
    /// <exception cref="ArgumentNullException">If any required argument is null.</exception>
    public HydrateChangeGroupAction(
        string deviceId,
        AudioChannelRegistry registry,
        ChangeGroupManager groupManager,
        CommandQueue queue,
        JsonRpcDispatcher dispatcher,
        LogonAction? logon)
        : this(deviceId, registry, zoneRegistry: null, groupManager, queue, dispatcher, logon)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HydrateChangeGroupAction"/> class
    /// with M4 zone-registry support — the zone-enable controlTags are
    /// added to the change-group subscription list alongside the M3
    /// level/mute/router tags.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="registry">The channel registry.</param>
    /// <param name="zoneRegistry">The optional zone registry; null when M4 is not yet wired.</param>
    /// <param name="groupManager">The change-group manager.</param>
    /// <param name="queue">The command queue.</param>
    /// <param name="dispatcher">The JSON-RPC dispatcher.</param>
    /// <param name="logon">Optional Logon action whose completion this action waits on.</param>
    /// <exception cref="ArgumentNullException">If any required argument is null.</exception>
    public HydrateChangeGroupAction(
        string deviceId,
        AudioChannelRegistry registry,
        AudioZoneRegistry? zoneRegistry,
        ChangeGroupManager groupManager,
        CommandQueue queue,
        JsonRpcDispatcher dispatcher,
        LogonAction? logon)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(groupManager);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(dispatcher);

        _deviceId = deviceId;
        _registry = registry;
        _zoneRegistry = zoneRegistry;
        _groupManager = groupManager;
        _queue = queue;
        _dispatcher = dispatcher;
        _logon = logon;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_logon is not null)
        {
            // Wait on Logon completion (success or skip) before subscribing.
            // The QSC Core docs note Logon-required errors only surface on
            // the next privileged command — the change-group subscribe IS
            // a privileged command, so racing it against Logon would cause
            // every reconnect to fail until the second AutoPoll cycle.
            await _logon.WaitForCompletionAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<AudioChannel> channels = _registry.GetAllChannels();
        IReadOnlyList<(string ChannelId, string ZoneId, string ControlTag)> zones =
            _zoneRegistry?.GetAll() ?? Array.Empty<(string, string, string)>();

        if (channels.Count == 0 && zones.Count == 0)
        {
            Log.Notice(_deviceId, "No audio channels or zone enables registered; skipping change-group subscribe.");
            return;
        }

        int subscribed = 0;
        foreach (AudioChannel channel in channels)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (TrySubscribe(channel.LevelTag, $"channel '{channel.Id}' level"))
            {
                subscribed++;
            }

            if (TrySubscribe(channel.MuteTag, $"channel '{channel.Id}' mute"))
            {
                subscribed++;
            }

            // M4: subscribe the output's routerTag (when configured).
            // Inputs and routerless outputs have an empty RouterTag.
            if (!channel.IsInput && !string.IsNullOrEmpty(channel.RouterTag))
            {
                if (TrySubscribe(channel.RouterTag, $"output '{channel.Id}' router"))
                {
                    subscribed++;
                }
            }
        }

        // M4: subscribe every (channelId, zoneId) pair's controlTag.
        foreach ((string channelId, string zoneId, string controlTag) in zones)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TrySubscribe(controlTag, $"zone '({channelId}, {zoneId})'"))
            {
                subscribed++;
            }
        }

        if (subscribed == 0)
        {
            Log.Warn(_deviceId, "Change-group hydration enqueued zero subscriptions; AutoPoll will not be issued.");
            return;
        }

        JsonRpcRequest autoPoll = _groupManager.BuildAutoPoll(ChangeGroupManager.PluginGroupId);
        _dispatcher.RegisterAutoPoll(autoPoll.Id, _groupManager);
        if (!_queue.TryEnqueue(autoPoll))
        {
            _dispatcher.UnregisterAutoPoll(autoPoll.Id);
            Log.Error(_deviceId, "Change-group AutoPoll request could not be enqueued.");
            return;
        }

        Log.Notice(_deviceId, $"Change group '{ChangeGroupManager.PluginGroupId}' subscribed ({subscribed} controls) at {ChangeGroupManager.DefaultAutoPollSeconds * 1000:0}ms AutoPoll.");
    }

    private bool TrySubscribe(string controlName, string ownerDescription)
    {
        // Group-cap rejection (BuildAddControl returns null) and queue
        // refusal (TryEnqueue returns false) are both observable here.
        // Log per-control failure so a partial hydration surfaces in
        // diagnostics; the previous M3 shape silently absorbed both
        // and only Logger.Warn'd the all-zero case.
        JsonRpcRequest? add = _groupManager.BuildAddControl(ChangeGroupManager.PluginGroupId, controlName);
        if (add is null)
        {
            Log.Warn(_deviceId, $"Change-group hydration could not build AddControl for {ownerDescription} (tag '{controlName}'); group cap reached?");
            return false;
        }

        if (!_queue.TryEnqueue(add))
        {
            Log.Warn(_deviceId, $"Change-group hydration could not enqueue AddControl for {ownerDescription} (tag '{controlName}'); queue refused.");
            return false;
        }

        return true;
    }
}
