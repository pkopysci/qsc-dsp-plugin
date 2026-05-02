// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using QscDspDevices.Connectivity.PostConnect;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.JsonRpc;
using Xunit;

namespace QscDspDevices.UnitTests.Connectivity.PostConnect;

/// <summary>
/// Unit tests for <see cref="LogonAction"/>. Pin: empty creds skip,
/// non-empty creds enqueue Logon and wait for response, error
/// response is non-fatal, timeout is non-fatal, completion task
/// settles in every path so HydrateChangeGroupAction can wait on it.
/// </summary>
public sealed class LogonActionTests
{
    [Fact]
    public async Task Empty_credentials_skip_Logon_and_complete_with_true()
    {
        using var queue = NewQueue();
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var sut = new LogonAction("dsp-1", () => LogonCredentials.Empty, queue, dispatcher, new IdGenerator());

        await sut.RunAsync(CancellationToken.None);

        queue.SnapshotPending().Should().BeEmpty();
        (await sut.WaitForCompletionAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task Configured_credentials_enqueue_a_well_formed_Logon_request()
    {
        using var queue = NewQueue();
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var ids = new IdGenerator();
        var sut = new LogonAction("dsp-1", () => new LogonCredentials("alice", "p4ss"), queue, dispatcher, ids);

        Task action = sut.RunAsync(CancellationToken.None);

        // We expect the Logon request to be enqueued before the action returns
        // (it then waits on the response). Drain to assert the wire shape.
        await Task.Delay(50);
        var sent = queue.SnapshotPending();
        sent.Should().HaveCount(1);
        sent[0].Method.Should().Be("Logon");
        var p = JObject.FromObject(sent[0].Params!);
        p["User"]!.ToString().Should().Be("alice");
        p["Password"]!.ToString().Should().Be("p4ss");

        // Inject a successful response to let the action complete.
        long id = sent[0].Id;
        FeedSuccess(dispatcher, id);
        await action;

        (await sut.WaitForCompletionAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task Error_response_marks_completion_false_but_returns_normally()
    {
        using var queue = NewQueue();
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var sut = new LogonAction("dsp-1", () => new LogonCredentials("u", "p"), queue, dispatcher, new IdGenerator());

        Task action = sut.RunAsync(CancellationToken.None);
        await Task.Delay(50);
        long id = queue.SnapshotPending()[0].Id;
        FeedError(dispatcher, id, code: 10, message: "Logon required");
        await action;

        (await sut.WaitForCompletionAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Cancellation_propagates()
    {
        using var queue = NewQueue();
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var sut = new LogonAction("dsp-1", () => new LogonCredentials("u", "p"), queue, dispatcher, new IdGenerator());

        using var cts = new CancellationTokenSource();
        Task action = sut.RunAsync(cts.Token);
        await Task.Delay(50);
        await cts.CancelAsync();

        Func<Task> act = async () => await action;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task WaitForCompletionAsync_before_first_run_returns_true()
    {
        using var queue = NewQueue();
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var sut = new LogonAction("dsp-1", () => LogonCredentials.Empty, queue, dispatcher, new IdGenerator());

        (await sut.WaitForCompletionAsync()).Should().BeTrue();
    }

    [Fact]
    public void Constructor_with_null_args_throws()
    {
        using var queue = NewQueue();
        var dispatcher = new JsonRpcDispatcher("dsp-1");
        var ids = new IdGenerator();
        Func<LogonCredentials> source = () => LogonCredentials.Empty;

        Action a = () => _ = new LogonAction(null!, source, queue, dispatcher, ids);
        Action b = () => _ = new LogonAction("d", null!, queue, dispatcher, ids);
        Action c = () => _ = new LogonAction("d", source, null!, dispatcher, ids);
        Action e = () => _ = new LogonAction("d", source, queue, null!, ids);
        Action f = () => _ = new LogonAction("d", source, queue, dispatcher, null!);

        a.Should().Throw<ArgumentNullException>();
        b.Should().Throw<ArgumentNullException>();
        c.Should().Throw<ArgumentNullException>();
        e.Should().Throw<ArgumentNullException>();
        f.Should().Throw<ArgumentNullException>();
    }

    private static CommandQueue NewQueue()
    {
        var q = new CommandQueue("dsp-1");
        q.StartAccepting();
        return q;
    }

    private static void FeedSuccess(JsonRpcDispatcher dispatcher, long id)
    {
        string json = $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":true}}";
        dispatcher.Dispatch(json);
    }

    private static void FeedError(JsonRpcDispatcher dispatcher, long id, int code, string message)
    {
        string json = $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"error\":{{\"code\":{code},\"message\":\"{message}\"}}}}";
        dispatcher.Dispatch(json);
    }
}
