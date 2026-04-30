// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-hardware-service/IAudioZoneEnabler.md
// Stub for the real type shipped in: gcu-hardware-service 4.3.4
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

using gcu_common_utils.GenericEventArgs;

namespace gcu_hardware_service.AudioDevices;

public interface IAudioZoneEnabler
{
    event EventHandler<GenericDualEventArgs<string, string>> AudioZoneEnableChanged;

    void AddAudioZoneEnable(string channelId, string zoneId, string controlTag);

    void RemoveAudioZoneEnable(string channelId, string zoneId);

    void ToggleAudioZoneEnable(string channelId, string zoneId);

    void SetAudioZoneEnable(string channelId, string zoneId, bool enable);

    bool QueryAudioZoneEnable(string channelId, string zoneId);
}
