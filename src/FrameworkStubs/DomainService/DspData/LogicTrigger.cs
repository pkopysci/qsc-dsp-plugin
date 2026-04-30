// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-domain-service/LogicTrigger.md
// Stub for the real type shipped in: gcu-domain-service 4.2.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

namespace gcu_domain_service.Data.DspData;

public class LogicTrigger : BaseData
{
    public string Label { get; set; } = string.Empty;

    public string TagName { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new List<string>();
}
