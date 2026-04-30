// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-hardware-service/IAudioControl.md
// Stub for the real type shipped in: gcu-hardware-service 4.3.4
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

using gcu_common_utils.GenericEventArgs;
using gcu_hardware_service.BaseDevice;

namespace gcu_hardware_service.AudioDevices;

public interface IAudioControl : IBaseDevice
{
    event EventHandler<GenericDualEventArgs<string, string>> AudioInputLevelChanged;

    event EventHandler<GenericDualEventArgs<string, string>> AudioInputMuteChanged;

    event EventHandler<GenericDualEventArgs<string, string>> AudioOutputLevelChanged;

    event EventHandler<GenericDualEventArgs<string, string>> AudioOutputMuteChanged;

    IEnumerable<string> GetAudioPresetIds();

    IEnumerable<string> GetAudioInputIds();

    IEnumerable<string> GetAudioOutputIds();

    void SetAudioInputLevel(string id, int level);

    int GetAudioInputLevel(string id);

    void SetAudioInputMute(string id, bool mute);

    bool GetAudioInputMute(string id);

    void SetAudioOutputLevel(string id, int level);

    int GetAudioOutputLevel(string id);

    void SetAudioOutputMute(string id, bool mute);

    bool GetAudioOutputMute(string id);

    void RecallAudioPreset(string id);

    void AddInputChannel(
        string id,
        string levelTag,
        string muteTag,
        int bankIndex,
        int levelMax,
        int levelMin,
        int routerIndex,
        List<string> tags);

    void AddOutputChannel(
        string id,
        string levelTag,
        string muteTag,
        string routerTag,
        int routerIndex,
        int bankIndex,
        int levelMax,
        int levelMin,
        List<string> tags);

    void AddPreset(string id, string bank, int index);
}
