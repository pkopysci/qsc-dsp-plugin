// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Concurrent;
using System.Collections.Generic;
using gcu_common_utils.GenericEventArgs;
using QscDspDevices.Transport;

namespace QscDspDevices.TestSupport.Transport;

/// <summary>
/// In-memory <see cref="IConnectionTransport"/> stub for unit tests of
/// the connection manager. Tests drive the lifecycle by calling
/// <see cref="SimulateConnectSuccess"/>, <see cref="SimulateConnectFailure"/>,
/// <see cref="SimulateReceive"/>, etc.
/// </summary>
/// <remarks>
/// This stub does NOT speak the QRC protocol — that's what
/// <c>FakeQrcServer</c> is for. The stub only models the bytes-on-the-
/// wire boundary of <see cref="IConnectionTransport"/>.
/// </remarks>
public sealed class StubTransport : IConnectionTransport
{
    private readonly ConcurrentQueue<byte[]> _sent = new();
    private bool _connected;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler<EventArgs>? Connected;

    /// <inheritdoc />
    public event EventHandler<GenericSingleEventArgs<string>>? ConnectionFailed;

    /// <inheritdoc />
    public event EventHandler<GenericSingleEventArgs<ReadOnlyMemory<byte>>>? RxReceived;

    /// <inheritdoc />
    public bool IsConnected => _connected;

    /// <summary>Gets the count of times <see cref="Connect"/> has been called.</summary>
    public int ConnectCallCount
    {
        get; private set;
    }

    /// <summary>Gets the count of times <see cref="Disconnect"/> has been called.</summary>
    public int DisconnectCallCount
    {
        get; private set;
    }

    /// <summary>Gets a snapshot of every payload passed to <see cref="Send"/>.</summary>
    public IReadOnlyList<byte[]> SentPayloads => _sent.ToArray();

    /// <inheritdoc />
    public void Connect()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ConnectCallCount++;
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        DisconnectCallCount++;
        _connected = false;
    }

    /// <inheritdoc />
    public void Send(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_connected)
        {
            throw new InvalidOperationException("Transport is not connected.");
        }

        _sent.Enqueue(payload);
    }

    /// <summary>
    /// Simulates a successful connect: flips <see cref="IsConnected"/> to
    /// true and fires <see cref="Connected"/>.
    /// </summary>
    public void SimulateConnectSuccess()
    {
        _connected = true;
        Connected?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Simulates a connect failure: fires <see cref="ConnectionFailed"/>
    /// with the supplied reason. <see cref="IsConnected"/> remains false.
    /// </summary>
    /// <param name="reason">The reason to surface in the event.</param>
    public void SimulateConnectFailure(string reason = "test-injected failure")
    {
        ConnectionFailed?.Invoke(this, new GenericSingleEventArgs<string>(reason));
    }

    /// <summary>
    /// Simulates a mid-flight disconnect: flips <see cref="IsConnected"/>
    /// to false and fires <see cref="ConnectionFailed"/>.
    /// </summary>
    /// <param name="reason">The reason to surface in the event.</param>
    public void SimulateMidFlightDrop(string reason = "test-injected drop")
    {
        _connected = false;
        ConnectionFailed?.Invoke(this, new GenericSingleEventArgs<string>(reason));
    }

    /// <summary>
    /// Simulates inbound bytes from the peer.
    /// </summary>
    /// <param name="bytes">The bytes to deliver to the receive-loop.</param>
    public void SimulateReceive(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        RxReceived?.Invoke(this, new GenericSingleEventArgs<ReadOnlyMemory<byte>>(bytes));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _connected = false;
        Connected = null;
        ConnectionFailed = null;
        RxReceived = null;
    }
}
