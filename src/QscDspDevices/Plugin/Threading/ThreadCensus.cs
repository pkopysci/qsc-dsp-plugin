// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace QscDspDevices.Plugin.Threading;

/// <summary>
/// Runtime guard that enforces the README §4 requirement of at most three
/// concurrently-alive plugin-owned threads (send-loop, receive-loop, timer).
/// </summary>
/// <remarks>
/// <para>
/// Each thread the plugin starts SHOULD call <see cref="Register"/> on
/// entry and <see cref="Unregister"/> on exit. If a fourth registration
/// is attempted while three are alive, the census logs <c>Logger.Error</c>
/// "thread budget breached" and (in DEBUG builds only) calls
/// <see cref="Environment.FailFast(string)"/> so the breach surfaces in
/// tests immediately. RELEASE builds log and return false (the README
/// forbids host crashes).
/// </para>
/// <para>
/// One <see cref="ThreadCensus"/> instance per plugin. Thread-safe.
/// </para>
/// </remarks>
public sealed class ThreadCensus
{
    /// <summary>The hard upper bound on concurrently-alive plugin threads, per README §4.</summary>
    public const int MaxThreads = 3;

    private readonly string _deviceId;
    private readonly object _lock = new();
    private readonly HashSet<int> _alive = new();
    private readonly Dictionary<int, string> _names = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadCensus"/> class
    /// tagged with the supplied device id (used in log messages).
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    public ThreadCensus(string deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        _deviceId = deviceId;
    }

    /// <summary>
    /// Gets the count of currently-registered plugin threads.
    /// </summary>
    public int AliveCount
    {
        get
        {
            lock (_lock)
            {
                return _alive.Count;
            }
        }
    }

    /// <summary>
    /// Attempts to register the calling thread under the supplied role
    /// name. Returns <c>false</c> (and refuses the registration) if doing
    /// so would exceed <see cref="MaxThreads"/>; in DEBUG builds also
    /// triggers <see cref="Environment.FailFast(string)"/> so violations
    /// surface immediately during tests.
    /// </summary>
    /// <param name="role">A short role descriptor, e.g. "send", "receive", "timer".</param>
    /// <returns><c>true</c> if registered; <c>false</c> if the budget is full.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="role"/> is null.</exception>
    public bool Register(string role)
    {
        ArgumentNullException.ThrowIfNull(role);

        int tid = Environment.CurrentManagedThreadId;
        lock (_lock)
        {
            if (_alive.Contains(tid))
            {
                // Idempotent: re-registering the same thread is harmless.
                return true;
            }

            if (_alive.Count >= MaxThreads)
            {
                string aliveList = string.Join(", ", _names.Values.Select(n => $"'{n}'"));
                string message = $"Thread budget breached: tried to register a {MaxThreads + 1}th plugin thread '{role}' while {MaxThreads} are alive ({aliveList}).";
                Log.Error(_deviceId, message);

#if DEBUG
                Environment.FailFast(message);
#endif

                return false;
            }

            _alive.Add(tid);
            _names[tid] = role;
            return true;
        }
    }

    /// <summary>
    /// Removes the calling thread from the registered set. Idempotent.
    /// </summary>
    public void Unregister()
    {
        int tid = Environment.CurrentManagedThreadId;
        lock (_lock)
        {
            _alive.Remove(tid);
            _names.Remove(tid);
        }
    }

    /// <summary>
    /// Returns a snapshot of the currently-registered role names. Test
    /// diagnostics; not used by production code.
    /// </summary>
    /// <returns>The role names in arbitrary order.</returns>
    public IReadOnlyList<string> Snapshot()
    {
        lock (_lock)
        {
            return _names.Values.ToArray();
        }
    }
}
