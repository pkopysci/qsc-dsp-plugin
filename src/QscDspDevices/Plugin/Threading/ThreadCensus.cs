// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace QscDspDevices.Plugin.Threading;

/// <summary>
/// Runtime guard that enforces the README §4 requirement of at most three
/// concurrently-alive plugin-owned units of work (M2: one async session
/// task; M3 will grow this to dedicated send/receive/timer threads).
/// </summary>
/// <remarks>
/// <para>
/// Each plugin-owned unit of work calls <see cref="Register"/> on entry
/// and disposes the returned <see cref="ThreadCensusRegistration"/> on
/// exit. The census issues opaque monotonic tokens rather than keying
/// on <see cref="Environment.CurrentManagedThreadId"/>, because async
/// continuations can resume on different threadpool threads — a
/// thread-id-keyed implementation would Unregister the wrong id and
/// leak the original entry.
/// </para>
/// <para>
/// If a fourth registration is attempted while three are alive, the
/// census logs <c>Logger.Error</c> "thread budget breached" and (in
/// DEBUG builds only) calls <see cref="Environment.FailFast(string)"/>
/// so the breach surfaces in tests immediately. RELEASE builds log and
/// return a sentinel registration whose <c>IsBudgetBreach</c> is
/// <c>true</c> (the README forbids host crashes).
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
    private readonly Dictionary<long, string> _alive = new();
    private long _nextToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadCensus"/> class
    /// tagged with the supplied device id (used in log messages).
    /// </summary>
    /// <param name="deviceId">The owning device id.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="deviceId"/> is null.</exception>
    public ThreadCensus(string deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        _deviceId = deviceId;
    }

    /// <summary>
    /// Gets the count of currently-registered plugin units of work.
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
    /// Attempts to register a new unit of work under the supplied role.
    /// Returns a disposable handle whose <see cref="ThreadCensusRegistration.Dispose"/>
    /// removes the entry. If registering would exceed
    /// <see cref="MaxThreads"/> the census logs <c>Logger.Error</c>
    /// "thread budget breached" and (in DEBUG only) triggers
    /// <see cref="Environment.FailFast(string)"/>. RELEASE callers
    /// receive a sentinel registration with
    /// <see cref="ThreadCensusRegistration.IsBudgetBreach"/> <c>true</c>;
    /// disposing it is a no-op.
    /// </summary>
    /// <param name="role">A short role descriptor, e.g. "session", "send", "receive", "timer".</param>
    /// <returns>The registration handle.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="role"/> is null.</exception>
    public ThreadCensusRegistration Register(string role)
    {
        ArgumentNullException.ThrowIfNull(role);

        lock (_lock)
        {
            if (_alive.Count >= MaxThreads)
            {
                string aliveList = string.Join(", ", _alive.Values.Select(n => $"'{n}'"));
                string breach = $"Thread budget breached: tried to register a {MaxThreads + 1}th plugin work item '{role}' while {MaxThreads} are alive ({aliveList}).";
                Log.Error(_deviceId, breach);

#if DEBUG
                Environment.FailFast(breach);
#endif

                return ThreadCensusRegistration.Breach;
            }

            long token = Interlocked.Increment(ref _nextToken);
            _alive[token] = role;
            return new ThreadCensusRegistration(this, token);
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
            return _alive.Values.ToArray();
        }
    }

    /// <summary>
    /// Removes the entry identified by <paramref name="token"/>. Called
    /// only by <see cref="ThreadCensusRegistration.Dispose"/>.
    /// </summary>
    /// <param name="token">The token returned from <see cref="Register"/>.</param>
    internal void RemoveByToken(long token)
    {
        lock (_lock)
        {
            _alive.Remove(token);
        }
    }
}
