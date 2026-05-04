// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.Protocol.Ecp;
using Xunit;

namespace QscDspDevices.UnitTests.Protocol.Ecp;

public sealed class EcpCommandTests
{
    [Fact]
    public void StatusGet_is_just_sg()
    {
        EcpCommand.StatusGet().Should().Be("sg");
    }

    [Fact]
    public void Login_quotes_both_arguments()
    {
        EcpCommand.Login("alice", "1234").Should().Be("login \"alice\" \"1234\"");
    }

    [Fact]
    public void Login_escapes_specials_in_name()
    {
        EcpCommand.Login("a b\"c", "p").Should().Be("login \"a b\\\"c\" \"p\"");
    }

    [Fact]
    public void ControlGet_quotes_the_id()
    {
        EcpCommand.ControlGet("gain1").Should().Be("cg \"gain1\"");
    }

    [Fact]
    public void ControlGet_escapes_quotes_in_id()
    {
        EcpCommand.ControlGet("My \"Gain\"").Should().Be("cg \"My \\\"Gain\\\"\"");
    }

    [Fact]
    public void ControlSetValue_uses_invariant_culture_for_doubles()
    {
        EcpCommand.ControlSetValue("g", 6.25).Should().Be("csv \"g\" 6.25");
    }

    [Fact]
    public void ControlSetString_quotes_both_args()
    {
        EcpCommand.ControlSetString("mute1", "true").Should().Be("css \"mute1\" \"true\"");
    }

    [Fact]
    public void ControlSetPosition_emits_csp()
    {
        EcpCommand.ControlSetPosition("g", 0.5).Should().Be("csp \"g\" 0.5");
    }

    [Fact]
    public void ControlTrigger_emits_ct()
    {
        EcpCommand.ControlTrigger("play").Should().Be("ct \"play\"");
    }

    [Fact]
    public void SnapshotLoad_emits_ssl_with_default_zero_ramp()
    {
        EcpCommand.SnapshotLoad("Bank A", 3).Should().Be("ssl \"Bank A\" 3 0");
    }

    [Fact]
    public void SnapshotLoad_emits_ramp_seconds_when_supplied()
    {
        EcpCommand.SnapshotLoad("snapshot1", 2, 5).Should().Be("ssl \"snapshot1\" 2 5");
    }

    [Fact]
    public void ChangeGroupCommands_emit_expected_wire_text()
    {
        EcpCommand.ChangeGroupCreate(1u).Should().Be("cgc 1");
        EcpCommand.ChangeGroupAdd(1u, "gain1").Should().Be("cga 1 \"gain1\"");
        EcpCommand.ChangeGroupScheduleNoAck(1u, 100).Should().Be("cgsna 1 100");
        EcpCommand.ChangeGroupDestroy(1u).Should().Be("cgd 1");
    }

    [Fact]
    public void Null_arguments_throw_ArgumentNullException()
    {
        ((Action)(() => EcpCommand.Login(null!, "p"))).Should().Throw<ArgumentNullException>();
        ((Action)(() => EcpCommand.Login("n", null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => EcpCommand.ControlGet(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => EcpCommand.ControlSetValue(null!, 0))).Should().Throw<ArgumentNullException>();
        ((Action)(() => EcpCommand.ControlSetString(null!, "v"))).Should().Throw<ArgumentNullException>();
        ((Action)(() => EcpCommand.ControlSetString("c", null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => EcpCommand.ControlSetPosition(null!, 0.5))).Should().Throw<ArgumentNullException>();
        ((Action)(() => EcpCommand.ControlTrigger(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => EcpCommand.SnapshotLoad(null!, 1))).Should().Throw<ArgumentNullException>();
        ((Action)(() => EcpCommand.ChangeGroupAdd(1u, null!))).Should().Throw<ArgumentNullException>();
    }
}
