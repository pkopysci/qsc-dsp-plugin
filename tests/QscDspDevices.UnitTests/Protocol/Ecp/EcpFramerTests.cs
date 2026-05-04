// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Text;
using FluentAssertions;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.Ecp;
using Xunit;

namespace QscDspDevices.UnitTests.Protocol.Ecp;

public sealed class EcpFramerTests
{
    [Fact]
    public void Encode_appends_a_single_LF()
    {
        byte[] encoded = EcpFramer.Encode("sg");
        encoded.Should().Equal(0x73, 0x67, 0x0A);
    }

    [Fact]
    public void Encode_throws_on_null()
    {
        Action act = () => EcpFramer.Encode(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Append_yields_one_frame_per_LF_terminated_segment()
    {
        var framer = new EcpFramer();
        IEnumerable<string> frames = framer.Append(Encoding.UTF8.GetBytes("line one\nline two\n"));
        frames.Should().Equal("line one", "line two");
        framer.PendingBytes.Should().Be(0);
    }

    [Fact]
    public void Append_strips_trailing_CR()
    {
        var framer = new EcpFramer();
        IEnumerable<string> frames = framer.Append(Encoding.UTF8.GetBytes("with-cr\r\nno-cr\n"));
        frames.Should().Equal("with-cr", "no-cr");
    }

    [Fact]
    public void Append_drops_empty_lines_silently()
    {
        var framer = new EcpFramer();
        IEnumerable<string> frames = framer.Append(Encoding.UTF8.GetBytes("a\n\n\nb\n"));
        frames.Should().Equal("a", "b");
    }

    [Fact]
    public void Append_buffers_partial_frame_across_calls()
    {
        var framer = new EcpFramer();
        framer.Append(Encoding.UTF8.GetBytes("hello, ")).Should().BeEmpty();
        framer.PendingBytes.Should().Be(7);
        IEnumerable<string> frames = framer.Append(Encoding.UTF8.GetBytes("world\n"));
        frames.Should().Equal("hello, world");
        framer.PendingBytes.Should().Be(0);
    }

    [Fact]
    public void Append_handles_a_lone_CR_inside_a_frame()
    {
        // \r without a following \n is just a literal byte inside the frame.
        var framer = new EcpFramer();
        IEnumerable<string> frames = framer.Append(Encoding.UTF8.GetBytes("a\rb\n"));
        frames.Should().Equal("a\rb");
    }

    [Fact]
    public void Append_throws_FrameTooLargeException_when_pending_exceeds_max()
    {
        var framer = new EcpFramer(initialCapacity: 16, maxFrameBytes: 16);
        byte[] tooBig = Encoding.UTF8.GetBytes(new string('x', 17));
        Action act = () => framer.Append(tooBig);
        act.Should().Throw<FrameTooLargeException>();
    }

    [Fact]
    public void Append_does_not_carry_pending_bytes_after_FrameTooLarge_throw()
    {
        var framer = new EcpFramer(initialCapacity: 16, maxFrameBytes: 16);
        byte[] tooBig = Encoding.UTF8.GetBytes(new string('x', 17));
        try
        {
            framer.Append(tooBig);
        }
        catch (FrameTooLargeException)
        {
            // expected
        }

        framer.PendingBytes.Should().Be(0, "the framer must reset its pending tail after the throw");
    }

    [Fact]
    public void Constructor_validates_arguments()
    {
        Action zero = () => { _ = new EcpFramer(0, 16); };
        zero.Should().Throw<ArgumentOutOfRangeException>();

        Action negativeMax = () => { _ = new EcpFramer(16, -1); };
        negativeMax.Should().Throw<ArgumentOutOfRangeException>();

        Action maxLessThanInitial = () => { _ = new EcpFramer(64, 16); };
        maxLessThanInitial.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Append_round_trips_quoting_specials_through_a_typical_sr_line()
    {
        // Practical: a typical sr response with quoted DESIGN_NAME ends with CRLF.
        var framer = new EcpFramer();
        IEnumerable<string> frames = framer.Append(Encoding.UTF8.GetBytes("sr \"My Design\" \"AbCdEf\" 1 1\r\n"));
        frames.Should().Equal("sr \"My Design\" \"AbCdEf\" 1 1");
    }
}
