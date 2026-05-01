// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Linq;
using QscDspDevices.Plugin;

namespace QscDspDevices.AudioControl;

/// <summary>
/// Thread-safe registry of audio channels and presets. The Crestron
/// framework calls <c>AddInputChannel</c>, <c>AddOutputChannel</c>, and
/// <c>AddPreset</c> at composition time (and occasionally afterwards
/// when a config refresh comes through); the registry is the single
/// source of truth for the rest of the plugin.
/// </summary>
/// <remarks>
/// The registry is intentionally a thin map. Cache and event-raising
/// live in <c>AudioControlService</c>; preset issue-on-recall
/// lives in <c>PresetService</c>; this type holds metadata only.
/// </remarks>
public sealed class AudioChannelRegistry
{
    private readonly string _deviceId;
    private readonly object _lock = new();
    private readonly Dictionary<string, AudioChannel> _channels = new();
    private readonly Dictionary<string, AudioPreset> _presets = new();
    private readonly Dictionary<string, string> _tagToChannelId = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioChannelRegistry"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id, used in log messages.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="deviceId"/> is null.</exception>
    public AudioChannelRegistry(string deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        _deviceId = deviceId;
    }

    /// <summary>
    /// Registers an input channel. Re-registering the same id replaces
    /// the prior entry and logs <c>Logger.Notice</c>; the framework is
    /// allowed to refresh metadata at runtime.
    /// </summary>
    /// <param name="channel">The channel definition.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="channel"/> is null.</exception>
    /// <exception cref="ArgumentException">If <paramref name="channel"/>.IsInput is false.</exception>
    public void RegisterInput(AudioChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        if (!channel.IsInput)
        {
            throw new ArgumentException("Channel must be an input.", nameof(channel));
        }

        Register(channel);
    }

    /// <summary>
    /// Registers an output channel. Re-registering replaces; see <see cref="RegisterInput"/>.
    /// </summary>
    /// <param name="channel">The channel definition.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="channel"/> is null.</exception>
    /// <exception cref="ArgumentException">If <paramref name="channel"/>.IsInput is true.</exception>
    public void RegisterOutput(AudioChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        if (channel.IsInput)
        {
            throw new ArgumentException("Channel must be an output.", nameof(channel));
        }

        Register(channel);
    }

    /// <summary>
    /// Registers a preset. Re-registering the same id replaces the
    /// prior entry and logs <c>Logger.Notice</c>.
    /// </summary>
    /// <param name="preset">The preset definition.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="preset"/> is null.</exception>
    public void RegisterPreset(AudioPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        lock (_lock)
        {
            if (_presets.ContainsKey(preset.Id))
            {
                Log.Notice(_deviceId, $"Re-registering preset '{preset.Id}'.");
            }

            _presets[preset.Id] = preset;
        }
    }

    /// <summary>
    /// Gets a snapshot of all registered input channel ids in registration order.
    /// </summary>
    /// <returns>The input channel ids.</returns>
    public IReadOnlyList<string> GetInputIds()
    {
        lock (_lock)
        {
            return _channels.Values.Where(c => c.IsInput).Select(c => c.Id).ToArray();
        }
    }

    /// <summary>
    /// Gets a snapshot of all registered output channel ids.
    /// </summary>
    /// <returns>The output channel ids.</returns>
    public IReadOnlyList<string> GetOutputIds()
    {
        lock (_lock)
        {
            return _channels.Values.Where(c => !c.IsInput).Select(c => c.Id).ToArray();
        }
    }

    /// <summary>
    /// Gets a snapshot of all registered preset ids.
    /// </summary>
    /// <returns>The preset ids.</returns>
    public IReadOnlyList<string> GetPresetIds()
    {
        lock (_lock)
        {
            return _presets.Keys.ToArray();
        }
    }

    /// <summary>
    /// Gets a snapshot of all registered channels (input + output).
    /// </summary>
    /// <returns>The channels.</returns>
    public IReadOnlyList<AudioChannel> GetAllChannels()
    {
        lock (_lock)
        {
            return _channels.Values.ToArray();
        }
    }

    /// <summary>
    /// Tries to fetch a channel by id.
    /// </summary>
    /// <param name="id">The channel id.</param>
    /// <param name="channel">The channel, if found.</param>
    /// <returns><c>true</c> if found.</returns>
    public bool TryGetChannel(string id, out AudioChannel? channel)
    {
        ArgumentNullException.ThrowIfNull(id);
        lock (_lock)
        {
            return _channels.TryGetValue(id, out channel);
        }
    }

    /// <summary>
    /// Tries to fetch a preset by id.
    /// </summary>
    /// <param name="id">The preset id.</param>
    /// <param name="preset">The preset, if found.</param>
    /// <returns><c>true</c> if found.</returns>
    public bool TryGetPreset(string id, out AudioPreset? preset)
    {
        ArgumentNullException.ThrowIfNull(id);
        lock (_lock)
        {
            return _presets.TryGetValue(id, out preset);
        }
    }

    /// <summary>
    /// Tries to look up a channel id by one of its tags (level or mute).
    /// AutoPoll deltas arrive keyed on tag name, not channel id, so the
    /// dispatch path needs this reverse lookup.
    /// </summary>
    /// <param name="tag">The level-tag or mute-tag string.</param>
    /// <param name="channelId">The owning channel id, if any.</param>
    /// <returns><c>true</c> if a channel owns this tag.</returns>
    public bool TryGetChannelIdByTag(string tag, out string? channelId)
    {
        ArgumentNullException.ThrowIfNull(tag);
        lock (_lock)
        {
            return _tagToChannelId.TryGetValue(tag, out channelId);
        }
    }

    private void Register(AudioChannel channel)
    {
        lock (_lock)
        {
            if (_channels.TryGetValue(channel.Id, out AudioChannel? existing))
            {
                Log.Notice(_deviceId, $"Re-registering channel '{channel.Id}'.");
                _tagToChannelId.Remove(existing.LevelTag);
                _tagToChannelId.Remove(existing.MuteTag);
            }

            _channels[channel.Id] = channel;
            _tagToChannelId[channel.LevelTag] = channel.Id;
            _tagToChannelId[channel.MuteTag] = channel.Id;
        }
    }
}
