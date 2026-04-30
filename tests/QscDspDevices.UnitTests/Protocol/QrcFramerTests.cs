// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Linq;
using System.Text;
using FluentAssertions;
using QscDspDevices.Protocol;
using Xunit;

namespace QscDspDevices.UnitTests.Protocol;

/// <summary>
/// Unit tests for <see cref="QrcFramer"/>. Verify null-byte framing for
/// every shape of inbound buffer the receive-loop will encounter, and that
/// the configured max-frame budget is enforced exactly.
/// </summary>
public sealed class QrcFramerTests
{
    [Fact]
    public void Append_with_two_complete_frames_in_one_buffer_emits_both_in_order()
    {
        var framer = new QrcFramer();
        byte[] input = JoinFrames("first", "second");

        var frames = framer.Append(input).ToArray();

        frames.Should().Equal("first", "second");
        framer.PendingBytes.Should().Be(0);
    }

    [Fact]
    public void Append_with_one_frame_split_across_two_reads_emits_one_complete_frame()
    {
        var framer = new QrcFramer();
        byte[] half1 = Encoding.UTF8.GetBytes("hel");
        byte[] half2 = Encoding.UTF8.GetBytes("lo").Concat(new byte[] { QrcFramer.FrameTerminator }).ToArray();

        var first = framer.Append(half1).ToArray();
        var second = framer.Append(half2).ToArray();

        first.Should().BeEmpty();
        second.Should().Equal("hello");
        framer.PendingBytes.Should().Be(0);
    }

    [Fact]
    public void Append_with_three_frames_in_two_reads_emits_them_in_order()
    {
        var framer = new QrcFramer();
        byte[] read1 = JoinFrames("a", "b").Concat(Encoding.UTF8.GetBytes("partial")).ToArray();
        byte[] read2 = new byte[] { QrcFramer.FrameTerminator };

        var first = framer.Append(read1).ToArray();
        var second = framer.Append(read2).ToArray();

        first.Should().Equal("a", "b");
        second.Should().Equal("partial");
    }

    [Fact]
    public void Empty_frame_between_terminators_is_emitted_as_empty_string()
    {
        var framer = new QrcFramer();
        byte[] input = new byte[] { QrcFramer.FrameTerminator };

        var frames = framer.Append(input).ToArray();

        frames.Should().Equal(string.Empty);
    }

    [Fact]
    public void Empty_input_is_a_no_op()
    {
        var framer = new QrcFramer();

        var frames = framer.Append(ReadOnlySpan<byte>.Empty).ToArray();

        frames.Should().BeEmpty();
        framer.PendingBytes.Should().Be(0);
    }

    [Fact]
    public void Frame_exactly_at_max_is_accepted()
    {
        const int max = 1024;
        var framer = new QrcFramer(initialCapacity: 64, maxFrameBytes: max);
        byte[] input = new byte[max + 1];
        for (int i = 0; i < max; i++)
        {
            input[i] = (byte)'x';
        }

        input[max] = QrcFramer.FrameTerminator;

        var frames = framer.Append(input).ToArray();

        frames.Should().HaveCount(1);
        frames[0].Length.Should().Be(max);
    }

    [Fact]
    public void Frame_one_byte_over_max_throws_FrameTooLargeException()
    {
        const int max = 256;
        var framer = new QrcFramer(initialCapacity: 64, maxFrameBytes: max);
        byte[] input = new byte[max + 1];
        for (int i = 0; i < max + 1; i++)
        {
            input[i] = (byte)'x';
        }

        var act = () => framer.Append(input).ToArray();

        act.Should().Throw<FrameTooLargeException>()
            .Where(ex => ex.LimitBytes == max && ex.AccumulatedBytes >= max + 1);
    }

    [Fact]
    public void After_FrameTooLarge_the_pending_buffer_is_cleared_so_subsequent_reads_recover()
    {
        const int max = 64;
        var framer = new QrcFramer(initialCapacity: 16, maxFrameBytes: max);
        byte[] huge = new byte[max + 1];
        for (int i = 0; i < huge.Length; i++)
        {
            huge[i] = (byte)'a';
        }

        Action act = () => framer.Append(huge);
        act.Should().Throw<FrameTooLargeException>();

        framer.PendingBytes.Should().Be(0);

        // After the exception, the framer should be usable again.
        var frames = framer.Append(JoinFrames("ok")).ToArray();
        frames.Should().Equal("ok");
    }

    [Fact]
    public void Encode_appends_a_single_terminator()
    {
        byte[] encoded = QrcFramer.Encode("{\"a\":1}");

        encoded.Should().HaveCount(8);
        encoded[^1].Should().Be(QrcFramer.FrameTerminator);
        Encoding.UTF8.GetString(encoded.AsSpan(0, encoded.Length - 1)).Should().Be("{\"a\":1}");
    }

    [Fact]
    public void Encode_null_throws_ArgumentNullException()
    {
        Action act = () => QrcFramer.Encode(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_rejects_non_positive_capacities()
    {
        Action zeroInitial = () => _ = new QrcFramer(initialCapacity: 0, maxFrameBytes: 1024);
        Action zeroMax = () => _ = new QrcFramer(initialCapacity: 16, maxFrameBytes: 0);
        Action maxLessThanInitial = () => _ = new QrcFramer(initialCapacity: 1024, maxFrameBytes: 16);

        zeroInitial.Should().Throw<ArgumentOutOfRangeException>();
        zeroMax.Should().Throw<ArgumentOutOfRangeException>();
        maxLessThanInitial.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Round_trip_encode_decode_preserves_payload_bytes()
    {
        var framer = new QrcFramer();
        const string original = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"NoOp\"}";
        byte[] encoded = QrcFramer.Encode(original);

        var frames = framer.Append(encoded).ToArray();

        frames.Should().Equal(original);
    }

    private static byte[] JoinFrames(params string[] payloads)
    {
        int total = 0;
        var encoded = new byte[payloads.Length][];
        for (int i = 0; i < payloads.Length; i++)
        {
            encoded[i] = Encoding.UTF8.GetBytes(payloads[i]);
            total += encoded[i].Length + 1;
        }

        var result = new byte[total];
        int offset = 0;
        for (int i = 0; i < encoded.Length; i++)
        {
            encoded[i].CopyTo(result, offset);
            offset += encoded[i].Length;
            result[offset] = QrcFramer.FrameTerminator;
            offset += 1;
        }

        return result;
    }
}
