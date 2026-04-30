// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-domain-service/Authentication.md
// Stub for the real type shipped in: gcu-domain-service 4.2.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

namespace gcu_domain_service.Data.ConnectionData;

public class Authentication : BaseData
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
