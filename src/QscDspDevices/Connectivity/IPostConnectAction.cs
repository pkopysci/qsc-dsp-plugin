// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;

namespace QscDspDevices.Connectivity;

/// <summary>
/// Hook invoked by the connection manager immediately after the transition
/// into <see cref="ConnectionState.Connected"/> completes. M2 ships a
/// no-op default; M3 onwards uses this seam to subscribe to ChangeGroups,
/// re-send <c>Logon</c> when credentials are configured, and refresh
/// the registered control state.
/// </summary>
public interface IPostConnectAction
{
    /// <summary>
    /// Performs the post-connect action.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the action is done.</returns>
    Task RunAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Default no-op implementation of <see cref="IPostConnectAction"/>.
/// Used by M2 until M3 introduces hydration.
/// </summary>
public sealed class NoopPostConnectAction : IPostConnectAction
{
    /// <inheritdoc />
    public Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
