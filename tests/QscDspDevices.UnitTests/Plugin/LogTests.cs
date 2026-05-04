// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Reflection;
using FluentAssertions;
using gcu_common_utils.Logging;
using QscDspDevices.TestSupport.Logging;
using Xunit;

namespace QscDspDevices.UnitTests.Plugin;

/// <summary>
/// Unit tests for the internal <see cref="QscDspDevices.Plugin.Log"/>
/// helper. Reflection is the simplest way in from the test assembly
/// since `Log` is internal — alternative is `[InternalsVisibleTo]`,
/// but a critic concern in M2 specifically argued against widening
/// the public surface.
/// </summary>
public sealed class LogTests
{
    [Fact]
    public void Notice_routes_to_Logger_OnLog_with_Notice_severity()
    {
        using var sink = new TestLoggerSink();
        InvokeLog("Notice", "dsp-1", "hello");

        sink.Captures.Should().ContainSingle(c =>
            c.Severity == LogSeverity.Notice
            && c.Id == "dsp-1"
            && c.Message == "hello");
    }

    [Fact]
    public void Debug_routes_to_Logger_OnLog_with_Debug_severity()
    {
        using var sink = new TestLoggerSink();
        InvokeLog("Debug", "dsp-1", "diagnostic");

        sink.Captures.Should().ContainSingle(c =>
            c.Severity == LogSeverity.Debug
            && c.Id == "dsp-1"
            && c.Message == "diagnostic");
    }

    [Fact]
    public void Error_routes_to_Logger_OnLog_with_Error_severity()
    {
        using var sink = new TestLoggerSink();
        InvokeLog("Error", "dsp-1", "boom");

        sink.Captures.Should().ContainSingle(c =>
            c.Severity == LogSeverity.Error && c.Message == "boom");
    }

    [Fact]
    public void Warn_routes_to_Logger_OnLog_with_Warn_severity()
    {
        using var sink = new TestLoggerSink();
        InvokeLog("Warn", "dsp-1", "watch out");

        sink.Captures.Should().ContainSingle(c =>
            c.Severity == LogSeverity.Warn && c.Message == "watch out");
    }

    private static void InvokeLog(string method, string deviceId, string message)
    {
        Type? logType = typeof(QscDspDevices.QscDspTcp).Assembly
            .GetType("QscDspDevices.Plugin.Log");
        logType.Should().NotBeNull("Log type must exist in the QscDspDevices assembly");
        MethodInfo? mi = logType!.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
        mi.Should().NotBeNull($"Log.{method} must exist as a public static method");
        mi!.Invoke(null, new object[] { deviceId, message });
    }
}
