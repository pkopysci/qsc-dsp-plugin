// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using QscDspDevices.Plugin;

namespace QscDspDevices.AudioControl;

/// <summary>
/// Thread-safe registry of <c>(channelId, zoneId) → controlTag</c>
/// triples for the <c>IAudioZoneEnabler</c> surface. Keyed on the
/// pair because one input channel can be in many zones; per the
/// framework spec (<c>framework-docs/gcu-hardware-service/IAudioZoneEnabler.md</c>),
/// a duplicate <c>(channelId, zoneId)</c> registration is dropped.
/// </summary>
public sealed class AudioZoneRegistry
{
    private readonly string _deviceId;
    private readonly object _lock = new();
    private readonly Dictionary<(string ChannelId, string ZoneId), string> _byPair = new();
    private readonly Dictionary<string, (string ChannelId, string ZoneId)> _byTag = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioZoneRegistry"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id, for log messages.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="deviceId"/> is null.</exception>
    public AudioZoneRegistry(string deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        _deviceId = deviceId;
    }

    /// <summary>
    /// Registers a zone-enable control. If a row with the same
    /// <c>(channelId, zoneId)</c> already exists, the new registration
    /// SHALL be dropped per the framework spec; the prior entry remains.
    /// We log <c>Logger.Notice</c> on the drop so it is observable.
    /// </summary>
    /// <param name="channelId">The owning input channel id.</param>
    /// <param name="zoneId">The zone id (free-form).</param>
    /// <param name="controlTag">The Q-SYS named control to set.</param>
    /// <returns><c>true</c> if registered; <c>false</c> if the pair already existed and the call was a no-op.</returns>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public bool TryRegister(string channelId, string zoneId, string controlTag)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        ArgumentNullException.ThrowIfNull(zoneId);
        ArgumentNullException.ThrowIfNull(controlTag);

        var key = (channelId, zoneId);
        lock (_lock)
        {
            if (_byPair.ContainsKey(key))
            {
                Log.Notice(
                    _deviceId,
                    $"Zone-enable registration for ({channelId}, {zoneId}) already exists; dropping new tag '{controlTag}' per IAudioZoneEnabler.AddAudioZoneEnable spec.");
                return false;
            }

            _byPair[key] = controlTag;
            _byTag[controlTag] = key;
        }

        return true;
    }

    /// <summary>
    /// Removes the row keyed on the supplied pair. Silent no-op when
    /// no such row exists.
    /// </summary>
    /// <param name="channelId">The owning input channel id.</param>
    /// <param name="zoneId">The zone id.</param>
    /// <returns><c>true</c> if a row was removed.</returns>
    public bool Remove(string channelId, string zoneId)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        ArgumentNullException.ThrowIfNull(zoneId);
        var key = (channelId, zoneId);
        lock (_lock)
        {
            if (_byPair.TryGetValue(key, out string? tag))
            {
                _byPair.Remove(key);
                _byTag.Remove(tag);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Tries to fetch the controlTag for a pair.
    /// </summary>
    /// <param name="channelId">The owning input channel id.</param>
    /// <param name="zoneId">The zone id.</param>
    /// <param name="controlTag">The control tag, if found.</param>
    /// <returns><c>true</c> if the pair is registered.</returns>
    public bool TryGet(string channelId, string zoneId, out string? controlTag)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        ArgumentNullException.ThrowIfNull(zoneId);
        lock (_lock)
        {
            return _byPair.TryGetValue((channelId, zoneId), out controlTag);
        }
    }

    /// <summary>
    /// Tries to fetch the owning <c>(channelId, zoneId)</c> pair for a
    /// controlTag. Used by the AutoPoll fan-out dispatcher.
    /// </summary>
    /// <param name="controlTag">The control tag from an AutoPoll delta.</param>
    /// <param name="pair">The pair, if found.</param>
    /// <returns><c>true</c> if this tag is registered.</returns>
    public bool TryGetPair(string controlTag, out (string ChannelId, string ZoneId) pair)
    {
        ArgumentNullException.ThrowIfNull(controlTag);
        lock (_lock)
        {
            return _byTag.TryGetValue(controlTag, out pair);
        }
    }

    /// <summary>
    /// Indicates whether the supplied control name was registered as
    /// a zone-enable controlTag.
    /// </summary>
    /// <param name="tag">The control name from an AutoPoll delta.</param>
    /// <returns><c>true</c> when this tag belongs to a zone enable.</returns>
    public bool IsZoneTag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        lock (_lock)
        {
            return _byTag.ContainsKey(tag);
        }
    }

    /// <summary>
    /// Gets a snapshot of every registered zone-enable triple.
    /// </summary>
    /// <returns>The triples (channelId, zoneId, controlTag).</returns>
    public IReadOnlyList<(string ChannelId, string ZoneId, string ControlTag)> GetAll()
    {
        lock (_lock)
        {
            var result = new List<(string, string, string)>(_byPair.Count);
            foreach (KeyValuePair<(string ChannelId, string ZoneId), string> kv in _byPair)
            {
                result.Add((kv.Key.ChannelId, kv.Key.ZoneId, kv.Value));
            }

            return result;
        }
    }
}
