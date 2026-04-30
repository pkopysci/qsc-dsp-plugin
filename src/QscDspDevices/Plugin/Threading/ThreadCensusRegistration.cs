// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

namespace QscDspDevices.Plugin.Threading;

/// <summary>
/// Disposable handle returned by <see cref="ThreadCensus.Register"/>.
/// Disposing the handle removes the entry from the census. Tokens are
/// opaque and disjoint from <see cref="Environment.CurrentManagedThreadId"/>
/// so async work that crosses thread boundaries between Register and
/// Dispose still releases the correct entry.
/// </summary>
public readonly struct ThreadCensusRegistration : IDisposable, IEquatable<ThreadCensusRegistration>
{
    private readonly ThreadCensus? _census;
    private readonly long _token;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadCensusRegistration"/>
    /// struct. Called only by <see cref="ThreadCensus.Register"/>.
    /// </summary>
    /// <param name="census">The owning census.</param>
    /// <param name="token">The unique token assigned by the census.</param>
    internal ThreadCensusRegistration(ThreadCensus census, long token)
    {
        _census = census;
        _token = token;
    }

    /// <summary>Gets the sentinel returned when <see cref="ThreadCensus.Register"/> rejects a registration because the budget is full.</summary>
    public static ThreadCensusRegistration Breach => default;

    /// <summary>
    /// Gets a value indicating whether this is the budget-breach sentinel
    /// (i.e. the census refused the registration). Disposing such a
    /// registration is a no-op.
    /// </summary>
    public bool IsBudgetBreach => _census is null;

    /// <summary>Equality operator.</summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>Whether the two handles refer to the same registration.</returns>
    public static bool operator ==(ThreadCensusRegistration left, ThreadCensusRegistration right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>Whether the two handles refer to different registrations.</returns>
    public static bool operator !=(ThreadCensusRegistration left, ThreadCensusRegistration right) => !left.Equals(right);

    /// <summary>
    /// Removes the entry from the owning census. Idempotent. Safe to
    /// call from any thread regardless of where <see cref="ThreadCensus.Register"/>
    /// was called.
    /// </summary>
    public void Dispose() => _census?.RemoveByToken(_token);

    /// <inheritdoc />
    public bool Equals(ThreadCensusRegistration other)
        => ReferenceEquals(_census, other._census) && _token == other._token;

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is ThreadCensusRegistration other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_census, _token);
}
