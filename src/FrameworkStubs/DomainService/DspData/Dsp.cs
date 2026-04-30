// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-domain-service/Dsp.md
// Stub for the real type shipped in: gcu-domain-service 4.2.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

using gcu_domain_service.Data.ConnectionData;

namespace gcu_domain_service.Data.DspData;

public class Dsp : BaseData
{
    public int CoreId
    {
        get; set;
    }

    public List<string> Dependencies { get; set; } = new List<string>();

    public Connection Connection { get; set; } = new Connection();

    public List<Preset> Presets { get; set; } = new List<Preset>();

    public List<LogicTrigger> LogicTriggers { get; set; } = new List<LogicTrigger>();
}
