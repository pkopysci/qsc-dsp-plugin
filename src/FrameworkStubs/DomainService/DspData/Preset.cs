// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-domain-service/DspPreset.md
// Stub for the real type shipped in: gcu-domain-service 4.2.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.
//
// IMPORTANT: per the framework documentation, `Tags` is a public field
// (not a property) in the current implementation. Mirror exactly.

namespace gcu_domain_service.Data.DspData;

public class Preset : BaseData
{
    public string Bank { get; set; } = string.Empty;

    public int Index
    {
        get; set;
    }

#pragma warning disable CA1051, SA1401 // Field is intentionally public per framework docs (DspPreset.md).
    public List<string> Tags = new List<string>();
#pragma warning restore CA1051, SA1401
}
