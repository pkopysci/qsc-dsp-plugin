// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace QscDspDevices.Protocol;

/// <summary>
/// Splits the inbound TCP byte stream from a Q-SYS Core into UTF-8 JSON
/// frames terminated by the ASCII NUL byte (<c>0x00</c>), and builds
/// outbound bytes for a string in the same format.
/// </summary>
/// <remarks>
/// QRC's wire framing is documented in <c>research/QRC_PROTOCOL.md §1.2</c>:
/// every JSON-RPC message is a UTF-8 object terminated by exactly one
/// <c>0x00</c>. There is no length prefix and no batched-array support.
///
/// The framer is stateful per-connection: it owns a growable buffer that
/// holds the partial-frame tail across <see cref="Append(ReadOnlySpan{byte})"/>
/// calls. The buffer doubles geometrically up to <see cref="MaxFrameBytes"/>;
/// past that it raises <see cref="FrameTooLargeException"/> to prevent an
/// unbounded allocation triggered by a hostile or buggy peer.
///
/// This type is NOT thread-safe; the receive-loop owns one instance and
/// is the sole caller.
/// </remarks>
public sealed class QrcFramer
{
    /// <summary>
    /// The default initial buffer capacity (16 KiB). The buffer doubles
    /// geometrically up to <see cref="DefaultMaxFrameBytes"/>.
    /// </summary>
    public const int DefaultInitialCapacity = 16 * 1024;

    /// <summary>
    /// The default maximum accumulated frame size (16 MiB). Frames larger
    /// than this raise <see cref="FrameTooLargeException"/>.
    /// </summary>
    public const int DefaultMaxFrameBytes = 16 * 1024 * 1024;

    /// <summary>The ASCII NUL byte used as the frame terminator.</summary>
    public const byte FrameTerminator = 0x00;

    private readonly ArrayBufferWriter<byte> _buffer;
    private readonly int _maxFrameBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="QrcFramer"/> class with
    /// the default capacities (<see cref="DefaultInitialCapacity"/> and
    /// <see cref="DefaultMaxFrameBytes"/>).
    /// </summary>
    public QrcFramer()
        : this(DefaultInitialCapacity, DefaultMaxFrameBytes)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QrcFramer"/> class with
    /// explicit capacities.
    /// </summary>
    /// <param name="initialCapacity">Initial backing-buffer capacity in bytes.</param>
    /// <param name="maxFrameBytes">Maximum accumulated bytes for a single
    /// frame before <see cref="FrameTooLargeException"/> is raised.</param>
    /// <exception cref="ArgumentOutOfRangeException">If
    /// <paramref name="initialCapacity"/> or <paramref name="maxFrameBytes"/>
    /// is non-positive, or if max is less than initial.</exception>
    public QrcFramer(int initialCapacity, int maxFrameBytes)
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

    /// <summary>
    /// Gets the number of bytes currently held in the framer's pending tail.
    /// </summary>
    public int PendingBytes => _buffer.WrittenCount;

    /// <summary>
    /// Encodes a single JSON payload to bytes terminated by <c>0x00</c>.
    /// </summary>
    /// <param name="json">The JSON string to encode (UTF-8).</param>
    /// <returns>The encoded bytes ready to write to the transport.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="json"/> is null.</exception>
    public static byte[] Encode(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        int byteCount = Encoding.UTF8.GetByteCount(json);
        var output = new byte[byteCount + 1];
        Encoding.UTF8.GetBytes(json, output.AsSpan(0, byteCount));
        output[byteCount] = FrameTerminator;
        return output;
    }

    /// <summary>
    /// Appends inbound bytes from the transport and returns each completed
    /// frame's decoded UTF-8 content. The framer's pending tail is updated
    /// to hold any partial frame still awaiting a terminator.
    /// </summary>
    /// <param name="bytes">The fresh bytes received from the transport.</param>
    /// <returns>An enumerable of completed frame payloads (terminator
    /// stripped). May be empty if the input contained no terminators.</returns>
    /// <exception cref="FrameTooLargeException">The pending tail plus the
    /// new bytes exceed <see cref="MaxFrameBytes"/> before a terminator
    /// arrives.</exception>
    public IEnumerable<string> Append(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return Array.Empty<string>();
        }

        // Scan the new bytes for terminators, copying segments into the
        // buffer one frame at a time. We materialize results into a list
        // so the caller can iterate without holding the framer in a
        // foreach across additional Append calls.
        var results = new List<string>();
        int start = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == FrameTerminator)
            {
                int segmentLength = i - start;
                if (segmentLength > 0)
                {
                    AppendToBuffer(bytes.Slice(start, segmentLength));
                }

                if (_buffer.WrittenCount > 0)
                {
                    results.Add(Encoding.UTF8.GetString(_buffer.WrittenSpan));
                    _buffer.Clear();
                }
                else
                {
                    // Empty frame between two terminators — emit empty
                    // string for spec faithfulness; consumers can decide.
                    results.Add(string.Empty);
                }

                start = i + 1;
            }
        }

        // Whatever's left after the last terminator is the new pending tail.
        if (start < bytes.Length)
        {
            AppendToBuffer(bytes[start..]);
        }

        if (_buffer.WrittenCount > _maxFrameBytes)
        {
            long observed = _buffer.WrittenCount;
            _buffer.Clear();
            throw new FrameTooLargeException(_maxFrameBytes, observed);
        }

        return results;
    }

    private void AppendToBuffer(ReadOnlySpan<byte> segment)
    {
        long projected = (long)_buffer.WrittenCount + segment.Length;
        if (projected > _maxFrameBytes)
        {
            long observed = projected;
            _buffer.Clear();
            throw new FrameTooLargeException(_maxFrameBytes, observed);
        }

        var dest = _buffer.GetSpan(segment.Length);
        segment.CopyTo(dest);
        _buffer.Advance(segment.Length);
    }
}
