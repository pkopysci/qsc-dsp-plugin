// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.JsonRpc;
using QscDspDevices.TestSupport.Logging;
using Xunit;

namespace QscDspDevices.UnitTests.Protocol;

/// <summary>
/// Unit tests for <see cref="JsonRpcDispatcher"/>. Verify id correlation,
/// AutoPoll subscription routing, server-notification handling, and
/// unknown-id logging.
/// </summary>
public sealed class JsonRpcDispatcherTests
{
    [Fact]
    public async Task Pending_request_completes_when_matching_response_arrives()
    {
        var sut = new JsonRpcDispatcher("dsp-1");
        Task<JsonRpcResponse> pending = sut.RegisterPending(42, CancellationToken.None);

        sut.Dispatch("""{"jsonrpc":"2.0","id":42,"result":{"ok":true}}""");

        JsonRpcResponse response = await pending;
        response.Id.Should().Be(42);
        response.Result.Should().NotBeNull();
        response.IsError.Should().BeFalse();
    }

    [Fact]
    public void Notification_fires_NotificationReceived_event()
    {
        var sut = new JsonRpcDispatcher("dsp-1");
        JsonRpcResponse? captured = null;
        sut.NotificationReceived += (_, args) => captured = args.Arg;

        sut.Dispatch("""{"jsonrpc":"2.0","method":"EngineStatus","params":{"State":"Active"}}""");

        captured.Should().NotBeNull();
        captured!.IsNotification.Should().BeTrue();
        captured.Method.Should().Be("EngineStatus");
    }

    [Fact]
    public void AutoPoll_push_routes_to_subscription_not_to_pending_request()
    {
        var sut = new JsonRpcDispatcher("dsp-1");
        var subscription = new RecordingSubscription();
        sut.RegisterAutoPoll(7, subscription);

        sut.Dispatch("""{"jsonrpc":"2.0","id":7,"result":{"Changes":[{"Name":"a","Value":1}]}}""");
        sut.Dispatch("""{"jsonrpc":"2.0","id":7,"result":{"Changes":[{"Name":"a","Value":2}]}}""");

        subscription.Pushed.Should().HaveCount(2);
        subscription.Pushed[0].Id.Should().Be(7);
    }

    [Fact]
    public async Task After_UnregisterAutoPoll_the_id_completes_a_one_shot_pending_request()
    {
        var sut = new JsonRpcDispatcher("dsp-1");
        var subscription = new RecordingSubscription();
        sut.RegisterAutoPoll(11, subscription);
        sut.UnregisterAutoPoll(11).Should().BeTrue();

        Task<JsonRpcResponse> pending = sut.RegisterPending(11, CancellationToken.None);
        sut.Dispatch("""{"jsonrpc":"2.0","id":11,"result":"ok"}""");

        JsonRpcResponse response = await pending;
        response.Id.Should().Be(11);
        subscription.Pushed.Should().BeEmpty();
    }

    [Fact]
    public void Unknown_id_is_logged_at_debug_and_dropped()
    {
        // Issue #22: demoted from Warn to Debug — stale responses
        // (e.g., late replies after a reconnect) are common and don't
        // warrant a Warn entry.
        using var sink = new TestLoggerSink();
        var sut = new JsonRpcDispatcher("dsp-1");

        sut.Dispatch("""{"jsonrpc":"2.0","id":999,"result":{"ok":true}}""");

        sink.Captures.Should().Contain(e =>
            e.Severity == gcu_common_utils.Logging.LogSeverity.Debug
            && e.Message.Contains("unknown id 999", StringComparison.Ordinal));
        sink.ContainsWarnMatching("unknown id 999").Should().BeFalse();
    }

    [Fact]
    public void Malformed_json_is_logged_at_error_and_swallowed()
    {
        using var sink = new TestLoggerSink();
        var sut = new JsonRpcDispatcher("dsp-1");

        sut.Dispatch("""{"this is not valid json""");

        sink.ContainsErrorMatching("Failed to deserialize").Should().BeTrue();
    }

    [Fact]
    public void Empty_frame_is_dispatched_as_no_op()
    {
        using var sink = new TestLoggerSink();
        var sut = new JsonRpcDispatcher("dsp-1");

        sut.Dispatch(string.Empty);

        sink.Captures.Should().BeEmpty();
    }

    [Fact]
    public void Frame_without_id_or_method_logs_warn()
    {
        using var sink = new TestLoggerSink();
        var sut = new JsonRpcDispatcher("dsp-1");

        sut.Dispatch("""{"jsonrpc":"2.0","result":"orphan"}""");

        sink.ContainsWarnMatching("neither id nor a notification method").Should().BeTrue();
    }

    [Fact]
    public async Task CancelAllPending_faults_every_outstanding_waiter()
    {
        var sut = new JsonRpcDispatcher("dsp-1");
        Task<JsonRpcResponse> t1 = sut.RegisterPending(1, CancellationToken.None);
        Task<JsonRpcResponse> t2 = sut.RegisterPending(2, CancellationToken.None);

        sut.CancelAllPending("connection-dropped");

        await FluentActions.Awaiting(async () => await t1).Should().ThrowAsync<OperationCanceledException>();
        await FluentActions.Awaiting(async () => await t2).Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void RegisterPending_with_duplicate_id_throws()
    {
        var sut = new JsonRpcDispatcher("dsp-1");
        _ = sut.RegisterPending(5, CancellationToken.None);

        Action act = () => sut.RegisterPending(5, CancellationToken.None);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RegisterAutoPoll_with_duplicate_id_throws()
    {
        var sut = new JsonRpcDispatcher("dsp-1");
        sut.RegisterAutoPoll(7, new RecordingSubscription());

        Action act = () => sut.RegisterAutoPoll(7, new RecordingSubscription());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ClearAutoPolls_drops_every_subscription_and_returns_the_count()
    {
        var sut = new JsonRpcDispatcher("dsp-1");
        sut.RegisterAutoPoll(1, new RecordingSubscription());
        sut.RegisterAutoPoll(2, new RecordingSubscription());
        sut.RegisterAutoPoll(3, new RecordingSubscription());

        int cleared = sut.ClearAutoPolls();

        cleared.Should().Be(3);

        // Re-register at the same ids without throwing — the prior
        // registrations are gone.
        Action reregister = () =>
        {
            sut.RegisterAutoPoll(1, new RecordingSubscription());
            sut.RegisterAutoPoll(2, new RecordingSubscription());
            sut.RegisterAutoPoll(3, new RecordingSubscription());
        };
        reregister.Should().NotThrow();
    }

    [Fact]
    public void ClearAutoPolls_on_empty_returns_zero()
    {
        var sut = new JsonRpcDispatcher("dsp-1");
        sut.ClearAutoPolls().Should().Be(0);
    }

    [Fact]
    public void Constructor_with_null_deviceId_throws()
    {
        Action act = () => _ = new JsonRpcDispatcher(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Dispatch_with_null_throws()
    {
        var sut = new JsonRpcDispatcher("dsp-1");
        Action act = () => sut.Dispatch(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class RecordingSubscription : IAutoPollSubscription
    {
        public List<JsonRpcResponse> Pushed { get; } = new();

        public void OnPush(JsonRpcResponse response) => Pushed.Add(response);
    }
}
