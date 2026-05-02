// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.Connectivity;
using QscDspDevices.Connectivity.PostConnect;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity.PostConnect;

/// <summary>
/// Unit tests for <see cref="CompositePostConnectAction"/>.
/// </summary>
public sealed class CompositePostConnectActionTests
{
    [Fact]
    public async Task Empty_list_completes_immediately()
    {
        var sut = new CompositePostConnectAction(Array.Empty<IPostConnectAction>());
        await sut.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Actions_run_in_declaration_order()
    {
        var order = new List<int>();
        var sut = new CompositePostConnectAction(new IPostConnectAction[]
        {
            new RecordingAction(order, 1),
            new RecordingAction(order, 2),
            new RecordingAction(order, 3),
        });

        await sut.RunAsync(CancellationToken.None);

        order.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task First_failing_action_short_circuits_the_chain()
    {
        var order = new List<int>();
        var sut = new CompositePostConnectAction(new IPostConnectAction[]
        {
            new RecordingAction(order, 1),
            new ThrowingAction(),
            new RecordingAction(order, 3),
        });

        Func<Task> act = async () => await sut.RunAsync(CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();

        order.Should().Equal(1);
    }

    [Fact]
    public void Constructor_with_null_throws()
    {
        Action act = () => _ = new CompositePostConnectAction(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class RecordingAction : IPostConnectAction
    {
        private readonly List<int> _log;
        private readonly int _id;

        public RecordingAction(List<int> log, int id)
        {
            _log = log;
            _id = id;
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            _log.Add(_id);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingAction : IPostConnectAction
    {
        public Task RunAsync(CancellationToken cancellationToken)
            => throw new InvalidOperationException("boom");
    }
}
