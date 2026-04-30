// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-domain-service/ComSpec.md
// Stub for the real type shipped in: gcu-domain-service 4.2.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

namespace gcu_domain_service.Data.ConnectionData;

public class ComSpec
{
    public string Protocol { get; set; } = string.Empty;

    public int BaudRate
    {
        get; set;
    }

    public int DataBits
    {
        get; set;
    }

    public int StopBits
    {
        get; set;
    }

    public string HwHandshake { get; set; } = string.Empty;

    public string SwHandshake { get; set; } = string.Empty;

    public string Parity { get; set; } = string.Empty;
}
