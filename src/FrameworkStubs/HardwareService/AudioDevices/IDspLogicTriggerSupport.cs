// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-hardware-service/IDspLogicTriggerSupport.md
// Stub for the real type shipped in: gcu-hardware-service 4.3.4
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

using gcu_common_utils.GenericEventArgs;

namespace gcu_hardware_service.AudioDevices;

public interface IDspLogicTriggerSupport
{
    event EventHandler<GenericSingleEventArgs<string>>? DspLogicTriggerStateChanged;

    void AddDspLogicTrigger(string id, string tagName, List<string> tags);

    void PulseDspLogicTrigger(string id);
}
