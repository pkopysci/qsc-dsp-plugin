// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-domain-service/Connection.md
// Stub for the real type shipped in: gcu-domain-service 4.2.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

namespace gcu_domain_service.Data.ConnectionData;

public class Connection : BaseData
{
    public string Transport { get; set; } = string.Empty;

    public string Driver { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public string BackupHost { get; set; } = string.Empty;

    public string MacAddress { get; set; } = string.Empty;

    public int Port
    {
        get; set;
    }

    public int BackupPort
    {
        get; set;
    }

    public Authentication Authentication { get; set; } = new Authentication();

    public ComSpec ComSpec { get; set; } = new ComSpec();
}
