// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-common-utils/Logger.md
// Stub for the real type shipped in: gcu-common-utils 4.3.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.
//
// EXCEPTION TO THE THROW-EVERYWHERE RULE:
// The four severity methods (Error/Warn/Notice/Debug) are no-op in the
// stub instead of throwing NotImplementedException. Rationale:
// production code calls these freely (and is REQUIRED to by README §2),
// and a NotImplementedException would crash any test that exercises a
// production path containing a logger call. The real DLL's behaviour
// is fire-and-forget logging — the stub matches that semantics. Tests
// that need to assert on log output use the static `OnLog` hook below
// (defined OUTSIDE the documented public surface so swapping in the
// real DLL silently ignores it).

using Crestron.SimplSharpPro;
using gcu_common_utils.Logging.LoggingTypes;

namespace gcu_common_utils.Logging;

public static class Logger
{
    /// <summary>
    /// Stub-only test hook: invoked on every Error/Warn/Notice/Debug call.
    /// Tests in QscDspDevices.TestSupport register a handler to capture
    /// log output for assertions. The real DLL has no equivalent and
    /// simply forwards to its Serilog sinks.
    /// </summary>
    public static event Action<LogSeverity, LogServiceTypes, LogDeviceTypes, string, string>? OnLog;

    public static bool IsInitialized => false;

    public static void Initialize(CrestronControlSystem controlSystem, string programId = "")
    {
        // No-op stub: real implementation initializes Serilog sinks.
        _ = controlSystem;
        _ = programId;
    }

    public static void EnableDebug()
    {
        // No-op stub.
    }

    public static void DisableDebug()
    {
        // No-op stub.
    }

    public static void Destroy()
    {
        // No-op stub.
    }

    public static void Error(LogServiceTypes service, LogDeviceTypes device, string id, string message)
        => OnLog?.Invoke(LogSeverity.Error, service, device, id, message);

    public static void Error(LogServiceTypes service, LogDeviceTypes device, string id, Exception exception)
        => OnLog?.Invoke(LogSeverity.Error, service, device, id, exception?.ToString() ?? string.Empty);

    public static void Warn(LogServiceTypes service, LogDeviceTypes device, string id, string message)
        => OnLog?.Invoke(LogSeverity.Warn, service, device, id, message);

    public static void Notice(LogServiceTypes service, LogDeviceTypes device, string id, string message)
        => OnLog?.Invoke(LogSeverity.Notice, service, device, id, message);

    public static void Debug(LogServiceTypes service, LogDeviceTypes device, string id, string message)
        => OnLog?.Invoke(LogSeverity.Debug, service, device, id, message);
}

/// <summary>
/// Stub-only severity enum used by the OnLog test hook. Has no equivalent
/// in the real DLL.
/// </summary>
public enum LogSeverity
{
    Error,
    Warn,
    Notice,
    Debug,
}
