// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;

namespace QscDspDevices.Protocol;

/// <summary>
/// Monotonically increasing 64-bit identifier source for outbound JSON-RPC
/// requests. Thread-safe; backed by <see cref="Interlocked.Increment(ref long)"/>.
/// </summary>
/// <remarks>
/// QRC requires every request to carry a unique <c>id</c>. The dispatcher
/// uses these ids to correlate responses to outstanding requests, and to
/// route <c>ChangeGroup.AutoPoll</c> push messages (which reuse the original
/// AutoPoll id) to their subscriptions.
/// </remarks>
public sealed class IdGenerator
{
    private long _last;

    /// <summary>
    /// Returns the next unique id. The first call returns <c>1</c>.
    /// </summary>
    /// <returns>A strictly increasing 64-bit integer.</returns>
    public long Next() => Interlocked.Increment(ref _last);
}
