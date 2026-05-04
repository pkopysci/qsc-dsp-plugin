// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace QscDspDevices.Protocol.Ecp;

/// <summary>
/// Splits the inbound TCP byte stream from a Q-SYS Core's ECP socket
/// into UTF-8 text frames terminated by <c>\n</c> (0x0A), and builds
/// outbound bytes for an ECP command in the same format.
/// </summary>
/// <remarks>
/// ECP framing is documented in <c>research/ECP_PROTOCOL.md §1.2</c>:
/// every command and response ends with the End-Of-Message character
/// <c>\n</c>. The Core terminates its responses with <c>\r\n</c>; the
/// framer strips a trailing <c>\r</c> from each frame. Empty lines
/// (back-to-back terminators or a trailing terminator after the last
/// frame) are dropped silently.
///
/// Stateful per-connection: owns a growable buffer that holds the
/// partial-frame tail across <see cref="Append(ReadOnlySpan{byte})"/>
/// calls. Doubles geometrically up to <see cref="MaxFrameBytes"/>; past
/// that it raises <see cref="FrameTooLargeException"/> to prevent an
/// unbounded allocation triggered by a hostile or buggy peer.
///
/// NOT thread-safe; the receive-loop owns one instance and is the sole
/// caller — same contract as <see cref="QrcFramer"/>.
/// </remarks>
internal sealed class EcpFramer
{
    /// <summary>The default initial buffer capacity (4 KiB).</summary>
    public const int DefaultInitialCapacity = 4 * 1024;

    /// <summary>
    /// The default maximum accumulated frame size (256 KiB). ECP frames
    /// are small ASCII lines; a 256 KiB cap is generous and still
    /// cheap to enforce.
    /// </summary>
    public const int DefaultMaxFrameBytes = 256 * 1024;

    /// <summary>The end-of-message byte (LF, 0x0A).</summary>
    public const byte FrameTerminator = 0x0A;

    /// <summary>The optional preceding CR (0x0D) the Core emits in <c>\r\n</c>.</summary>
    public const byte CarriageReturn = 0x0D;

    private readonly ArrayBufferWriter<byte> _buffer;
    private readonly int _maxFrameBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpFramer"/> class
    /// with the default capacities.
    /// </summary>
    public EcpFramer()
        : this(DefaultInitialCapacity, DefaultMaxFrameBytes)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcpFramer"/> class
    /// with explicit capacities.
    /// </summary>
    /// <param name="initialCapacity">Initial backing-buffer capacity in bytes.</param>
    /// <param name="maxFrameBytes">Maximum accumulated bytes for a single
    /// frame before <see cref="FrameTooLargeException"/> is raised.</param>
    /// <exception cref="ArgumentOutOfRangeException">If
    /// <paramref name="initialCapacity"/> or <paramref name="maxFrameBytes"/>
    /// is non-positive, or if max is less than initial.</exception>
    public EcpFramer(int initialCapacity, int maxFrameBytes)
    {
        if (initialCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), initialCapacity, "Initial capacity must be positive.");
        }

        if (maxFrameBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFrameBytes), maxFrameBytes, "Max frame size must be positive.");
        }

        if (maxFrameBytes < initialCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFrameBytes), maxFrameBytes, "Max frame size must not be less than initial capacity.");
        }

        _buffer = new ArrayBufferWriter<byte>(initialCapacity);
        _maxFrameBytes = maxFrameBytes;
    }

    /// <summary>Gets the configured maximum frame size, in bytes.</summary>
    public int MaxFrameBytes => _maxFrameBytes;

    /// <summary>Gets the number of bytes currently held in the framer's pending tail.</summary>
    public int PendingBytes => _buffer.WrittenCount;

    /// <summary>
    /// Encodes a single ECP command text to bytes terminated by <c>\n</c>.
    /// The input MUST NOT already contain a trailing terminator — callers
    /// pass the bare command text (e.g. <c>"sg"</c> or <c>"csv id4 6.2"</c>).
    /// </summary>
    /// <param name="command">The command text to encode (UTF-8).</param>
    /// <returns>The encoded bytes ready to write to the transport.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="command"/> is null.</exception>
    public static byte[] Encode(string command)
    {
        ArgumentNullException.ThrowIfNull(command);

        int byteCount = Encoding.UTF8.GetByteCount(command);
        byte[] output = new byte[byteCount + 1];
        Encoding.UTF8.GetBytes(command, output.AsSpan(0, byteCount));
        output[byteCount] = FrameTerminator;
        return output;
    }

    /// <summary>
    /// Appends inbound bytes from the transport and returns each
    /// completed frame's decoded text. A trailing <c>\r</c> on each
    /// frame is stripped. Empty lines are silently dropped.
    /// </summary>
    /// <param name="bytes">The fresh bytes received from the transport.</param>
    /// <returns>An enumerable of completed frame payloads (terminator stripped). May be empty.</returns>
    /// <exception cref="FrameTooLargeException">The pending tail plus the
    /// new bytes exceed <see cref="MaxFrameBytes"/> before a terminator
    /// arrives.</exception>
    public IEnumerable<string> Append(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return Array.Empty<string>();
        }

        var results = new List<string>();
        int start = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != FrameTerminator)
            {
                continue;
            }

            int segmentLength = i - start;
            if (segmentLength > 0)
            {
                AppendToBuffer(bytes.Slice(start, segmentLength));
            }

            if (_buffer.WrittenCount > 0)
            {
                ReadOnlySpan<byte> frameBytes = _buffer.WrittenSpan;
                if (frameBytes[^1] == CarriageReturn)
                {
                    frameBytes = frameBytes[..^1];
                }

                if (!frameBytes.IsEmpty)
                {
                    results.Add(Encoding.UTF8.GetString(frameBytes));
                }

                _buffer.ResetWrittenCount();
            }

            start = i + 1;
        }

        if (start < bytes.Length)
        {
            AppendToBuffer(bytes[start..]);
        }

        return results;
    }

    private void AppendToBuffer(ReadOnlySpan<byte> segment)
    {
        if (_buffer.WrittenCount + segment.Length > _maxFrameBytes)
        {
            int wouldBe = _buffer.WrittenCount + segment.Length;
            _buffer.ResetWrittenCount();
            throw new FrameTooLargeException(
                $"ECP frame exceeded maximum size: pending tail + new bytes would be {wouldBe} bytes (max {_maxFrameBytes}).");
        }

        Span<byte> destination = _buffer.GetSpan(segment.Length);
        segment.CopyTo(destination);
        _buffer.Advance(segment.Length);
    }
}
