// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-domain-service/Channel.md
// Stub for the real type shipped in: gcu-domain-service 4.2.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

namespace gcu_domain_service.Data.DspData;

public class Channel : BaseData
{
    public string LevelControlTag { get; set; } = string.Empty;

    public string MuteControlTag { get; set; } = string.Empty;

    public string RouterControlTag { get; set; } = string.Empty;

    public string DspId { get; set; } = string.Empty;

    public int RouterIndex
    {
        get; set;
    }

    public int BankIndex
    {
        get; set;
    }

    public string Label { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public int LevelMax
    {
        get; set;
    }

    public int LevelMin
    {
        get; set;
    }

    public List<ZoneEnableToggle> ZoneEnableToggles { get; set; } = new List<ZoneEnableToggle>();

    public List<string> Tags { get; set; } = new List<string>();
}
