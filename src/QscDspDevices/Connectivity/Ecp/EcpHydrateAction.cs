// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using QscDspDevices.AudioControl;
using QscDspDevices.LogicTriggers;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol.Ecp;

namespace QscDspDevices.Connectivity.Ecp;

/// <summary>
/// ECP equivalent of <c>HydrateChangeGroupAction</c>: runs once after
/// auth completes (or anonymous-mode is detected) on a fresh
/// connection. Builds a single change group, registers every named
/// control across the framework registries, and starts a 2-second
/// no-ack poll. Subsequent <c>cv</c> deltas flow through
/// <see cref="EcpAutoPollSubscription"/> to update the service-tier
/// caches.
/// </summary>
internal sealed class EcpHydrateAction
{
    /// <summary>The fixed change-group id used for the plugin's poll.</summary>
    public const uint PluginGroupId = 1;

    /// <summary>The ECP auto-poll period in milliseconds.</summary>
    public const int AutoPollPeriodMs = 2000;

    private readonly string _deviceId;
    private readonly EcpCommandQueue _queue;
    private readonly AudioChannelRegistry _channels;
    private readonly AudioZoneRegistry _zones;
    private readonly LogicTriggerRegistry _triggers;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpHydrateAction"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="queue">The ECP command queue.</param>
    /// <param name="channels">The audio-channel registry.</param>
    /// <param name="zones">The audio-zone registry.</param>
    /// <param name="triggers">The logic-trigger registry.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public EcpHydrateAction(
        string deviceId,
        EcpCommandQueue queue,
        AudioChannelRegistry channels,
        AudioZoneRegistry zones,
        LogicTriggerRegistry triggers)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(channels);
        ArgumentNullException.ThrowIfNull(zones);
        ArgumentNullException.ThrowIfNull(triggers);

        _deviceId = deviceId;
        _queue = queue;
        _channels = channels;
        _zones = zones;
        _triggers = triggers;
    }

    /// <summary>
    /// Sends the hydrate sequence: <c>cgc 1</c>, then <c>cga 1 "tag"</c>
    /// for every registered named control, then <c>cgsna 1 2000</c>.
    /// Empty registries produce just the <c>cgc</c> + <c>cgsna</c>
    /// pair (the change group exists but is empty until controls are
    /// added; the Core polls and emits no deltas).
    /// </summary>
    public void Run()
    {
        IReadOnlyList<string> tags = CollectTags();

        _queue.TryEnqueue(EcpCommand.ChangeGroupCreate(PluginGroupId));
        foreach (string tag in tags)
        {
            _queue.TryEnqueue(EcpCommand.ChangeGroupAdd(PluginGroupId, tag));
        }

        _queue.TryEnqueue(EcpCommand.ChangeGroupScheduleNoAck(PluginGroupId, AutoPollPeriodMs));

        Log.Notice(_deviceId, $"ECP hydrate: cgc 1 + {tags.Count} cga + cgsna {AutoPollPeriodMs}ms scheduled.");
    }

#pragma warning disable SA1204 // Static members should appear before non-static members
    private static void AddIfUnique(HashSet<string> seen, List<string> tags, string tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return;
        }

        if (seen.Add(tag))
        {
            tags.Add(tag);
        }
    }
#pragma warning restore SA1204

    private List<string> CollectTags()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var tags = new List<string>();

        foreach (AudioChannel channel in _channels.GetAllChannels())
        {
            AddIfUnique(seen, tags, channel.LevelTag);
            AddIfUnique(seen, tags, channel.MuteTag);
            AddIfUnique(seen, tags, channel.RouterTag);
        }

        foreach ((string _, string _, string controlTag) in _zones.GetAll())
        {
            AddIfUnique(seen, tags, controlTag);
        }

        foreach ((string _, string tagName) in _triggers.GetAll())
        {
            AddIfUnique(seen, tags, tagName);
        }

        return tags;
    }
}
