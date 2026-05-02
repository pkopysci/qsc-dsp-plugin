// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using QscDspDevices.Plugin;

namespace QscDspDevices.LogicTriggers;

/// <summary>
/// Thread-safe registry of <c>id → tagName</c> for the
/// <see cref="gcu_hardware_service.AudioDevices.IDspLogicTriggerSupport"/>
/// surface. Tracks the reverse <c>tagName → id</c> map for the
/// AutoPoll fan-out dispatcher.
/// </summary>
/// <remarks>
/// Re-registering the same id replaces and logs <c>Logger.Notice</c>;
/// this mirrors the M3 <c>AudioChannelRegistry</c> shape (the
/// framework spec is silent on duplicate-id behaviour for triggers).
/// </remarks>
public sealed class LogicTriggerRegistry
{
    private readonly string _deviceId;
    private readonly object _lock = new();
    private readonly Dictionary<string, string> _idToTag = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _tagToId = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="LogicTriggerRegistry"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id, for log messages.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="deviceId"/> is null.</exception>
    public LogicTriggerRegistry(string deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        _deviceId = deviceId;
    }

    /// <summary>
    /// Registers a logic trigger. Re-registering the same <paramref name="id"/>
    /// replaces the prior tag and logs <c>Logger.Notice</c>.
    /// </summary>
    /// <param name="id">The framework trigger id.</param>
    /// <param name="tagName">The Q-SYS named control to fire on Pulse.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public void Register(string id, string tagName)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(tagName);

        lock (_lock)
        {
            if (_idToTag.TryGetValue(id, out string? priorTag))
            {
                Log.Notice(_deviceId, $"Re-registering logic trigger '{id}': '{priorTag}' → '{tagName}'.");
                _tagToId.Remove(priorTag);
            }

            _idToTag[id] = tagName;
            _tagToId[tagName] = id;
        }
    }

    /// <summary>
    /// Tries to fetch the registered tagName for a trigger id.
    /// </summary>
    /// <param name="id">The trigger id.</param>
    /// <param name="tagName">The tag, if found.</param>
    /// <returns><c>true</c> if registered.</returns>
    public bool TryGet(string id, out string? tagName)
    {
        ArgumentNullException.ThrowIfNull(id);
        lock (_lock)
        {
            return _idToTag.TryGetValue(id, out tagName);
        }
    }

    /// <summary>
    /// Tries to fetch the trigger id for a tagName. Used by the
    /// AutoPoll fan-out dispatcher to route deltas to the trigger
    /// service.
    /// </summary>
    /// <param name="tagName">The control name from an AutoPoll delta.</param>
    /// <param name="id">The owning trigger id, if any.</param>
    /// <returns><c>true</c> if this tag belongs to a registered trigger.</returns>
    public bool TryGetIdByTag(string tagName, out string? id)
    {
        ArgumentNullException.ThrowIfNull(tagName);
        lock (_lock)
        {
            return _tagToId.TryGetValue(tagName, out id);
        }
    }

    /// <summary>
    /// Indicates whether the supplied control name was registered as a trigger tag.
    /// </summary>
    /// <param name="tag">The control name from an AutoPoll delta.</param>
    /// <returns><c>true</c> when this tag belongs to a registered trigger.</returns>
    public bool IsTriggerTag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        lock (_lock)
        {
            return _tagToId.ContainsKey(tag);
        }
    }

    /// <summary>
    /// Gets a snapshot of every registered <c>(id, tagName)</c> pair.
    /// </summary>
    /// <returns>The pairs.</returns>
    public IReadOnlyList<(string Id, string TagName)> GetAll()
    {
        lock (_lock)
        {
            var result = new List<(string, string)>(_idToTag.Count);
            foreach (KeyValuePair<string, string> kv in _idToTag)
            {
                result.Add((kv.Key, kv.Value));
            }

            return result;
        }
    }
}
