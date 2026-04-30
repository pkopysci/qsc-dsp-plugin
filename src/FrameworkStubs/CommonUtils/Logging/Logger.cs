// Stub source — public surface transcribed from framework-docs.
// Spec source: framework-docs/gcu-common-utils/Logger.md
// Stub for the real type shipped in: gcu-common-utils 4.3.3
// DO NOT EDIT to add behaviour — replace with the real NuGet package at delivery time.

using Crestron.SimplSharpPro;
using gcu_common_utils.Logging.LoggingTypes;

namespace gcu_common_utils.Logging;

public static class Logger
{
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

    public static void Error(LogServiceTypes service, LogDeviceTypes device, string id, string message) =>
        throw new NotImplementedException(
            "FrameworkStubs.Logger.Error(string) — replace stub with real gcu-common-utils package.");

    public static void Error(LogServiceTypes service, LogDeviceTypes device, string id, Exception exception) =>
        throw new NotImplementedException(
            "FrameworkStubs.Logger.Error(Exception) — replace stub with real gcu-common-utils package.");

    public static void Warn(LogServiceTypes service, LogDeviceTypes device, string id, string message) =>
        throw new NotImplementedException(
            "FrameworkStubs.Logger.Warn — replace stub with real gcu-common-utils package.");

    public static void Notice(LogServiceTypes service, LogDeviceTypes device, string id, string message) =>
        throw new NotImplementedException(
            "FrameworkStubs.Logger.Notice — replace stub with real gcu-common-utils package.");

    public static void Debug(LogServiceTypes service, LogDeviceTypes device, string id, string message) =>
        throw new NotImplementedException(
            "FrameworkStubs.Logger.Debug — replace stub with real gcu-common-utils package.");
}
