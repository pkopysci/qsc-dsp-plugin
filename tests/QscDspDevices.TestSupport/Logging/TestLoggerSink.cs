// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using gcu_common_utils.Logging;
using gcu_common_utils.Logging.LoggingTypes;

namespace QscDspDevices.TestSupport.Logging;

/// <summary>
/// In-memory sink for the framework <see cref="Logger"/> calls made during
/// tests. Subscribes to the stub-only <see cref="Logger.OnLog"/> hook on
/// construction and unsubscribes on disposal so each test can assert on
/// the logs produced by the production code under test.
/// </summary>
/// <remarks>
/// This type only works against the FrameworkStubs <see cref="Logger"/>
/// implementation (which exposes the test hook). It is never shipped.
/// Tests typically <c>using var sink = new TestLoggerSink();</c> at the
/// top of a method, then use <see cref="Captures"/> or
/// <see cref="ContainsErrorMatching"/> to assert.
/// </remarks>
public sealed class TestLoggerSink : IDisposable
{
    private readonly ConcurrentQueue<LoggedEntry> _entries = new();
    private readonly Action<LogSeverity, LogServiceTypes, LogDeviceTypes, string, string> _handler;
    private bool _disposed;

    /// <summary>
    /// Initializes a new sink and subscribes to <see cref="Logger.OnLog"/>.
    /// </summary>
    public TestLoggerSink()
    {
        _handler = OnLog;
        Logger.OnLog += _handler;
    }

    /// <summary>Gets a snapshot of every log entry captured so far.</summary>
    public IReadOnlyList<LoggedEntry> Captures => _entries.ToArray();

    /// <summary>
    /// Returns <c>true</c> if any captured Error entry's message contains
    /// the supplied substring (case-sensitive).
    /// </summary>
    /// <param name="substring">The substring to search for.</param>
    /// <returns><c>true</c> if found.</returns>
    public bool ContainsErrorMatching(string substring)
        => _entries.Any(e => e.Severity == LogSeverity.Error && e.Message.Contains(substring, StringComparison.Ordinal));

    /// <summary>
    /// Returns <c>true</c> if any captured Warn entry's message contains
    /// the supplied substring (case-sensitive).
    /// </summary>
    /// <param name="substring">The substring to search for.</param>
    /// <returns><c>true</c> if found.</returns>
    public bool ContainsWarnMatching(string substring)
        => _entries.Any(e => e.Severity == LogSeverity.Warn && e.Message.Contains(substring, StringComparison.Ordinal));

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Logger.OnLog -= _handler;
    }

    private void OnLog(LogSeverity severity, LogServiceTypes service, LogDeviceTypes device, string id, string message)
        => _entries.Enqueue(new LoggedEntry(severity, service, device, id, message));
}
