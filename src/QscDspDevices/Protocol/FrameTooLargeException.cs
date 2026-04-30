// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

namespace QscDspDevices.Protocol;

/// <summary>
/// Raised when an inbound QRC frame's accumulated bytes exceed the
/// configured maximum (default 16 MiB). Causes the connection manager to
/// drop the socket and reconnect, preventing an unbounded allocation
/// caused by a malformed or hostile peer.
/// </summary>
public sealed class FrameTooLargeException : Exception
{
    // The three constructors below exist purely to satisfy CA1032
    // ("Implement standard exception constructors"). The plugin never
    // throws the exception via these forms — only the (int, long)
    // overload is meaningful. Excluded from coverage so the 90% gate
    // tracks behaviour, not BCL-mandated ceremony.

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameTooLargeException"/> class
    /// with default values (limit zero, accumulated zero).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public FrameTooLargeException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameTooLargeException"/> class
    /// with the supplied message.
    /// </summary>
    /// <param name="message">The diagnostic message.</param>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public FrameTooLargeException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameTooLargeException"/> class
    /// with the supplied message and inner exception.
    /// </summary>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="innerException">The inner exception, if any.</param>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public FrameTooLargeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameTooLargeException"/> class
    /// with the configured limit and the observed accumulated size.
    /// </summary>
    /// <param name="limitBytes">The maximum frame size, in bytes.</param>
    /// <param name="accumulatedBytes">The number of bytes accumulated when
    /// the limit was exceeded.</param>
    public FrameTooLargeException(int limitBytes, long accumulatedBytes)
        : base($"QRC frame exceeded the {limitBytes}-byte maximum (observed at least {accumulatedBytes} bytes without a 0x00 terminator).")
    {
        LimitBytes = limitBytes;
        AccumulatedBytes = accumulatedBytes;
    }

    /// <summary>Gets the configured frame-size limit, in bytes.</summary>
    public int LimitBytes
    {
        get;
    }

    /// <summary>Gets the accumulated unframed bytes observed when the limit tripped.</summary>
    public long AccumulatedBytes
    {
        get;
    }
}
