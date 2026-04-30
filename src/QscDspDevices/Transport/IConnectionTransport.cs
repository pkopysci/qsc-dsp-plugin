// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using gcu_common_utils.GenericEventArgs;

namespace QscDspDevices.Transport;

/// <summary>
/// Abstraction over the byte-oriented TCP transport used by the QRC
/// protocol layer. Implementations include
/// <see cref="BasicTcpClientTransport"/> (production, wraps the
/// framework's <c>BasicTcpClient</c>) and a <c>RawTcpTransport</c>
/// in TestSupport (used by integration tests against the
/// <c>FakeQrcServer</c> to avoid invoking the framework stub).
/// </summary>
/// <remarks>
/// <para>
/// The transport is intentionally thin: the protocol layer owns framing
/// and JSON-RPC semantics. The transport's responsibilities are limited
/// to opening the socket, writing raw bytes, surfacing inbound bytes
/// via the <see cref="RxReceived"/> event, and reporting connection
/// failures via <see cref="ConnectionFailed"/>.
/// </para>
/// <para>
/// Implementations MUST NOT throw across the plugin boundary on
/// connection failure. Failures are reported via <see cref="ConnectionFailed"/>
/// so the connection manager can drive its state machine.
/// </para>
/// <para>
/// Event payloads use the framework's <see cref="GenericSingleEventArgs{T}"/>
/// for consistency with <c>BasicTcpClient</c>'s own event signatures.
/// </para>
/// </remarks>
public interface IConnectionTransport : IDisposable
{
    /// <summary>
    /// Raised when the underlying socket transitions to connected.
    /// </summary>
    event EventHandler<EventArgs>? Connected;

    /// <summary>
    /// Raised when the transport detects a connection failure (initial
    /// connect failure, mid-flight drop, etc.). The event arg contains
    /// an opaque description of the failure for logging.
    /// </summary>
    event EventHandler<GenericSingleEventArgs<string>>? ConnectionFailed;

    /// <summary>
    /// Raised when fresh bytes are received from the remote peer.
    /// </summary>
    event EventHandler<GenericSingleEventArgs<ReadOnlyMemory<byte>>>? RxReceived;

    /// <summary>
    /// Gets a value indicating whether the transport is currently
    /// connected.
    /// </summary>
    bool IsConnected
    {
        get;
    }

    /// <summary>
    /// Begins connecting to the configured remote endpoint. Returns
    /// without blocking; success or failure is reported asynchronously
    /// via the <see cref="Connected"/> or <see cref="ConnectionFailed"/>
    /// events.
    /// </summary>
    void Connect();

    /// <summary>
    /// Closes the active connection (if any). Idempotent.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Sends raw bytes to the remote peer. Caller must ensure the
    /// transport is connected.
    /// </summary>
    /// <param name="payload">The bytes to send. Must not be null.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="payload"/>
    /// is null.</exception>
    /// <exception cref="InvalidOperationException">If the transport is
    /// not currently connected.</exception>
    void Send(byte[] payload);
}
