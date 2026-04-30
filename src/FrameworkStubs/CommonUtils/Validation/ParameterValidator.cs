// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-common-utils/ParameterValidator.md
// Stub for the real type shipped in: gcu-common-utils 4.3.3
// Real behaviour implemented per spec: this is one of the two trivial helpers
// the framework-stubs spec explicitly permits to ship with real logic.
// DO NOT EDIT to add behaviour beyond what is documented — replace with the
// real NuGet package at delivery time.

using System.Globalization;

namespace gcu_common_utils.Validation;

public static class ParameterValidator
{
    public static void ThrowIfNull(object? param, string methodName, string paramName)
    {
        if (param is null)
        {
            throw new ArgumentNullException(
                paramName,
                string.Format(CultureInfo.InvariantCulture, "{0}() - {1} cannot be null.", methodName, paramName));
        }
    }

    public static void ThrowIfNullOrEmpty(string param, string methodName, string paramName)
    {
        if (string.IsNullOrEmpty(param))
        {
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, "{0}() - {1} cannot be null or empty.", methodName, paramName),
                paramName);
        }
    }
}
