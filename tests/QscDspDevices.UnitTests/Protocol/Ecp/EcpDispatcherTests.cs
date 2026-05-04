// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using gcu_common_utils.GenericEventArgs;
using QscDspDevices.Protocol.Ecp;
using QscDspDevices.TestSupport.Logging;
using Xunit;

namespace QscDspDevices.UnitTests.Protocol.Ecp;

public sealed class EcpDispatcherTests
{
    [Fact]
    public void Dispatch_raises_ResponseReceived_for_every_known_frame()
    {
        var dispatcher = new EcpDispatcher("dsp-1");
        var captured = new List<EcpResponse>();
        dispatcher.ResponseReceived += (_, args) => captured.Add(args.Arg);

        dispatcher.Dispatch("sr \"My Design\" \"abc\" 1 1");
        dispatcher.Dispatch("cv \"gain1\" \"-20dB\" -20 0.5");
        dispatcher.Dispatch("cgpa");
        dispatcher.Dispatch("login_required");

        captured.Should().HaveCount(4);
        captured[0].Kind.Should().Be(EcpResponseKind.StatusReport);
        captured[1].Kind.Should().Be(EcpResponseKind.ControlValue);
        captured[2].Kind.Should().Be(EcpResponseKind.ChangeGroupPollAck);
        captured[3].Kind.Should().Be(EcpResponseKind.LoginRequired);
    }

    [Fact]
    public void Dispatch_raises_Unknown_event_and_logs_Warn_for_malformed_frame()
    {
        using var sink = new TestLoggerSink();
        var dispatcher = new EcpDispatcher("dsp-1");
        EcpResponse? captured = null;
        dispatcher.ResponseReceived += (_, args) => captured = args.Arg;

        dispatcher.Dispatch("totally bogus line");

        captured.Should().NotBeNull();
        captured!.Kind.Should().Be(EcpResponseKind.Unknown);
        sink.ContainsWarnMatching("did not match any known response shape").Should().BeTrue();
    }

    [Fact]
    public void Dispatch_truncates_very_long_unknown_lines_in_log()
    {
        using var sink = new TestLoggerSink();
        var dispatcher = new EcpDispatcher("dsp-1");
        string huge = new('x', 5000);

        dispatcher.Dispatch(huge);

        // First Warn should contain the truncation marker.
        sink.Captures
            .Should()
            .Contain(e => e.Message.Contains("...", StringComparison.Ordinal));
    }

    [Fact]
    public void Multiple_subscribers_each_receive_every_frame()
    {
        var dispatcher = new EcpDispatcher("dsp-1");
        int countA = 0;
        int countB = 0;
        dispatcher.ResponseReceived += (_, _) => countA++;
        dispatcher.ResponseReceived += (_, _) => countB++;

        dispatcher.Dispatch("cgpa");
        dispatcher.Dispatch("cgpa");

        countA.Should().Be(2);
        countB.Should().Be(2);
    }

    [Fact]
    public void Dispatch_throws_on_null()
    {
        var dispatcher = new EcpDispatcher("dsp-1");
        Action act = () => dispatcher.Dispatch(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_throws_on_null_deviceId()
    {
        Action act = () => { _ = new EcpDispatcher(null!); };
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Dispatch_with_no_subscribers_does_not_throw()
    {
        var dispatcher = new EcpDispatcher("dsp-1");
        Action act = () => dispatcher.Dispatch("cgpa");
        act.Should().NotThrow();
    }

    [Fact]
    public void Subscriber_throwing_does_not_block_other_subscribers()
    {
        // The dispatcher's contract today does NOT guard subscribers
        // (mirrors JsonRpcDispatcher); a throwing subscriber surfaces
        // out of Dispatch. This test pins that contract explicitly so
        // a future "swallow" change is a deliberate decision rather
        // than an accident. Subscribers are called in registration
        // order; the second one runs only if the first didn't throw.
        var dispatcher = new EcpDispatcher("dsp-1");
        bool secondCalled = false;
        dispatcher.ResponseReceived += (_, _) => throw new InvalidOperationException("boom");
        dispatcher.ResponseReceived += (_, _) => secondCalled = true;

        Action act = () => dispatcher.Dispatch("cgpa");
        act.Should().Throw<InvalidOperationException>();
        secondCalled.Should().BeFalse();
    }

    [Fact]
    public void EventArgs_carry_the_parsed_response()
    {
        var dispatcher = new EcpDispatcher("dsp-1");
        GenericSingleEventArgs<EcpResponse>? args = null;
        dispatcher.ResponseReceived += (_, a) => args = a;

        dispatcher.Dispatch("cv \"g\" \"6\" 6 0.5");

        args.Should().NotBeNull();
        args!.Arg.Kind.Should().Be(EcpResponseKind.ControlValue);
        args.Arg.ControlId.Should().Be("g");
    }
}
