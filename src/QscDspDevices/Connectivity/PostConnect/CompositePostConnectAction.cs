// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QscDspDevices.Connectivity.PostConnect;

/// <summary>
/// Runs a list of <see cref="IPostConnectAction"/>s in declaration order.
/// The connection manager only takes a single hook; this composite lets
/// M3+ chain Logon → ChangeGroup hydration without modifying the M2
/// hook signature.
/// </summary>
public sealed class CompositePostConnectAction : IPostConnectAction
{
    private readonly IReadOnlyList<IPostConnectAction> _actions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositePostConnectAction"/> class.
    /// </summary>
    /// <param name="actions">The actions to run, in order.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="actions"/> is null.</exception>
    public CompositePostConnectAction(IReadOnlyList<IPostConnectAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        _actions = actions;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (IPostConnectAction action in _actions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action.RunAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
