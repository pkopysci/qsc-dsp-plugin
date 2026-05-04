// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.Protocol.Ecp;
using Xunit;

namespace QscDspDevices.UnitTests.Protocol.Ecp;

public sealed class EcpQuotingTests
{
    [Fact]
    public void Escape_passes_through_simple_ascii()
    {
        EcpQuoting.Escape("plain").Should().Be("plain");
        EcpQuoting.Escape(string.Empty).Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("\n", @"\n")]
    [InlineData("\r", @"\r")]
    [InlineData("\"", "\\\"")]
    [InlineData("\\", @"\\")]
    public void Escape_replaces_each_special_character(string raw, string escaped)
    {
        EcpQuoting.Escape(raw).Should().Be(escaped);
    }

    [Fact]
    public void Escape_handles_combined_specials()
    {
        EcpQuoting.Escape("multi\r\nline\"with\\slash").Should().Be(@"multi\r\nline\""with\\slash");
    }

    [Theory]
    [InlineData(@"plain", "plain")]
    [InlineData(@"\n", "\n")]
    [InlineData(@"\r", "\r")]
    [InlineData("\\\"", "\"")]
    [InlineData(@"\\", "\\")]
    public void Unescape_reverses_each_special(string escaped, string raw)
    {
        EcpQuoting.Unescape(escaped).Should().Be(raw);
    }

    [Fact]
    public void Unescape_passes_through_unrecognised_sequences()
    {
        // \t is not in the ECP escape table; pass through verbatim.
        EcpQuoting.Unescape(@"a\tb").Should().Be(@"a\tb");
    }

    [Fact]
    public void Unescape_at_end_with_dangling_backslash_passes_through()
    {
        EcpQuoting.Unescape(@"abc\").Should().Be(@"abc\");
    }

    [Theory]
    [InlineData("hello world")]
    [InlineData("multi\r\nline\"with\\slash")]
    [InlineData("Q-Sys named control")]
    [InlineData("")]
    public void Unescape_round_trips_Escape(string input)
    {
        EcpQuoting.Unescape(EcpQuoting.Escape(input)).Should().Be(input);
    }

    [Fact]
    public void Escape_throws_on_null()
    {
        Action act = () => EcpQuoting.Escape(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Unescape_throws_on_null()
    {
        Action act = () => EcpQuoting.Unescape(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
