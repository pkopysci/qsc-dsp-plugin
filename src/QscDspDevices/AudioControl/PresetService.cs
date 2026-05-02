// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using QscDspDevices.Plugin;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.AudioControl;

/// <summary>
/// Implements <c>RecallAudioPreset</c> by issuing a QRC
/// <c>Snapshot.Load</c> request against the registered preset's
/// snapshot bank and index.
/// </summary>
public sealed class PresetService
{
    private readonly string _deviceId;
    private readonly AudioChannelRegistry _registry;
    private readonly CommandQueue _queue;
    private readonly IdGenerator _ids;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresetService"/> class.
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="registry">The preset registry (shared with audio channels).</param>
    /// <param name="queue">The command queue requests are enqueued on.</param>
    /// <param name="ids">The shared monotonic id generator.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public PresetService(
        string deviceId,
        AudioChannelRegistry registry,
        CommandQueue queue,
        IdGenerator ids)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(ids);

        _deviceId = deviceId;
        _registry = registry;
        _queue = queue;
        _ids = ids;
    }

    /// <summary>
    /// Recalls the named preset by issuing <c>Snapshot.Load { Name = bank, Bank = index }</c>.
    /// Logs <c>Logger.Error</c> and returns silently on unknown id (the
    /// framework gives us a void return; callers expect best-effort
    /// behaviour).
    /// </summary>
    /// <param name="presetId">The framework preset id.</param>
    public void Recall(string presetId)
    {
        ArgumentNullException.ThrowIfNull(presetId);
        if (!_registry.TryGetPreset(presetId, out AudioPreset? preset) || preset is null)
        {
            Log.Error(_deviceId, $"RecallAudioPreset called with unknown preset id '{presetId}'.");
            return;
        }

        var request = new JsonRpcRequest
        {
            Id = _ids.Next(),
            Method = "Snapshot.Load",
            Params = new { Name = preset.Bank, Bank = preset.Index },
        };

        _queue.TryEnqueue(request);
    }
}
