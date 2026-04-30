// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using gcu_common_utils.Logging;
using gcu_common_utils.Logging.LoggingTypes;

namespace QscDspDevices.Plugin;

/// <summary>
/// Thin convenience wrapper over <see cref="Logger"/> that pre-fills the
/// service and device categories with the values every QscDspDevices
/// log line needs (<see cref="LogServiceTypes.Hardware"/>,
/// <see cref="LogDeviceTypes.Dsp"/>). Reduces visual noise at call
/// sites and makes it harder to log to the wrong category by accident.
/// </summary>
/// <remarks>
/// All <c>Logger.*</c> calls in production code SHOULD route through
/// <see cref="Log"/> rather than calling <see cref="Logger"/> directly.
/// </remarks>
internal static class Log
{
    /// <summary>Logs an Error-level message tagged to the supplied device id.</summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="message">The message to log.</param>
    public static void Error(string deviceId, string message)
        => Logger.Error(LogServiceTypes.Hardware, LogDeviceTypes.Dsp, deviceId, message);

    /// <summary>Logs a Warn-level message tagged to the supplied device id.</summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="message">The message to log.</param>
    public static void Warn(string deviceId, string message)
        => Logger.Warn(LogServiceTypes.Hardware, LogDeviceTypes.Dsp, deviceId, message);

    /// <summary>Logs a Notice-level message tagged to the supplied device id.</summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="message">The message to log.</param>
    public static void Notice(string deviceId, string message)
        => Logger.Notice(LogServiceTypes.Hardware, LogDeviceTypes.Dsp, deviceId, message);

    /// <summary>Logs a Debug-level message tagged to the supplied device id.</summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <param name="message">The message to log.</param>
    public static void Debug(string deviceId, string message)
        => Logger.Debug(LogServiceTypes.Hardware, LogDeviceTypes.Dsp, deviceId, message);
}
