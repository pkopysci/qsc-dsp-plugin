// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-common-utils/DataFormatter.md
// Stub for the real type shipped in: gcu-common-utils 4.3.3
// Real behaviour implemented per spec: this is one of the two trivial helpers
// the framework-stubs spec explicitly permits to ship with real logic.
// DO NOT EDIT to add behaviour beyond what is documented — replace with the
// real NuGet package at delivery time.

using System.Globalization;

namespace gcu_common_utils.Validation;

public static class DataFormatter
{
    public static string NormalizeDeviceModel(string arg)
    {
        if (string.IsNullOrEmpty(arg))
        {
            return string.Empty;
        }

        return arg.Trim().Replace("-", string.Empty, StringComparison.Ordinal).ToUpper(CultureInfo.InvariantCulture);
    }
}
