// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.Protocol.Ecp;
using Xunit;

namespace QscDspDevices.UnitTests.Protocol.Ecp;

public sealed class EcpResponseTests
{
    [Fact]
    public void Sentinel_lines_parse_to_their_kind()
    {
        EcpResponse.Parse("cgpa").Kind.Should().Be(EcpResponseKind.ChangeGroupPollAck);
        EcpResponse.Parse("login_required").Kind.Should().Be(EcpResponseKind.LoginRequired);
        EcpResponse.Parse("login_success").Kind.Should().Be(EcpResponseKind.LoginSuccess);
        EcpResponse.Parse("login_failed").Kind.Should().Be(EcpResponseKind.LoginFailed);
        EcpResponse.Parse("core_not_active").Kind.Should().Be(EcpResponseKind.CoreNotActive);
        EcpResponse.Parse("control_read_only").Kind.Should().Be(EcpResponseKind.ControlReadOnly);
        EcpResponse.Parse("too_many_change_groups").Kind.Should().Be(EcpResponseKind.TooManyChangeGroups);
    }

    [Fact]
    public void StatusReport_extracts_all_four_fields()
    {
        EcpResponse r = EcpResponse.Parse("sr \"My Design\" \"AbCdEf\" 1 1");
        r.Kind.Should().Be(EcpResponseKind.StatusReport);
        r.DesignName.Should().Be("My Design");
        r.DesignId.Should().Be("AbCdEf");
        r.IsPrimary.Should().Be(1);
        r.IsActive.Should().Be(1);
    }

    [Fact]
    public void StatusReport_with_zero_flags_indicates_standby()
    {
        EcpResponse r = EcpResponse.Parse("sr \"d\" \"id\" 0 0");
        r.IsPrimary.Should().Be(0);
        r.IsActive.Should().Be(0);
    }

    [Fact]
    public void ControlValue_extracts_id_display_value_position()
    {
        EcpResponse r = EcpResponse.Parse("cv \"gain1\" \"-100dB\" -100 0");
        r.Kind.Should().Be(EcpResponseKind.ControlValue);
        r.ControlId.Should().Be("gain1");
        r.Display.Should().Be("-100dB");
        r.Value.Should().Be(-100);
        r.Position.Should().Be(0);
    }

    [Fact]
    public void ControlValue_uses_invariant_culture_for_doubles()
    {
        EcpResponse r = EcpResponse.Parse("cv \"g\" \"6.25\" 6.25 0.5");
        r.Value.Should().Be(6.25);
        r.Position.Should().Be(0.5);
    }

    [Fact]
    public void BadId_extracts_the_offending_control_id()
    {
        EcpResponse r = EcpResponse.Parse("bad_id \"unknown\"");
        r.Kind.Should().Be(EcpResponseKind.BadId);
        r.ControlId.Should().Be("unknown");
    }

    [Fact]
    public void Bad_change_group_handle_parses_to_its_kind()
    {
        EcpResponse.Parse("bad_change_group_handle 7").Kind.Should().Be(EcpResponseKind.BadChangeGroupHandle);
    }

    [Fact]
    public void Unrecognised_line_yields_Unknown_with_raw_preserved()
    {
        EcpResponse r = EcpResponse.Parse("totally bogus line 1 2 3");
        r.Kind.Should().Be(EcpResponseKind.Unknown);
        r.Raw.Should().Be("totally bogus line 1 2 3");
    }

    [Fact]
    public void StatusReport_with_non_numeric_flag_falls_back_to_Unknown()
    {
        EcpResponse.Parse("sr \"d\" \"id\" yes 1").Kind.Should().Be(EcpResponseKind.Unknown);
    }

    [Fact]
    public void ControlValue_with_escaped_quote_in_display_string()
    {
        // Display string contains an escaped quote; tokenizer must
        // unescape it without ending the quoted token early.
        EcpResponse r = EcpResponse.Parse("cv \"g\" \"a\\\"b\" 1 0");
        r.Display.Should().Be("a\"b");
        r.Value.Should().Be(1);
    }

    [Fact]
    public void Parse_throws_on_null()
    {
        ((Action)(() => EcpResponse.Parse(null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Empty_line_parses_to_Unknown()
    {
        EcpResponse.Parse(string.Empty).Kind.Should().Be(EcpResponseKind.Unknown);
    }
}
