// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using gcu_common_utils.Logging;
using gcu_common_utils.Logging.LoggingTypes;

namespace QscDspDevices.TestSupport.Logging;

/// <summary>A single captured log entry from <see cref="TestLoggerSink"/>.</summary>
/// <param name="Severity">The severity level.</param>
/// <param name="Service">The service category.</param>
/// <param name="Device">The device category.</param>
/// <param name="Id">The owning device id.</param>
/// <param name="Message">The logged message.</param>
public sealed record LoggedEntry(
    LogSeverity Severity,
    LogServiceTypes Service,
    LogDeviceTypes Device,
    string Id,
    string Message);
