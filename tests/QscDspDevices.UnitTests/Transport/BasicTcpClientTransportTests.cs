// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.Transport;
using Xunit;

namespace QscDspDevices.UnitTests.Transport;

/// <summary>
/// Unit tests for <see cref="BasicTcpClientTransport"/>. Coverage of
/// the production transport is limited by the framework stub: the
/// underlying <c>BasicTcpClient.Connect</c>/<c>Disconnect</c>/<c>Send</c>
/// throw <c>NotImplementedException</c> in the stub, so the integration
/// tests use <c>RawTcpTransport</c> for end-to-end scenarios. These
/// unit tests cover the parts of <c>BasicTcpClientTransport</c> that
/// don't traverse the throwing stub methods: the constructor's argument
/// validation (which the stub's ctor faithfully implements), the
/// Dispose pattern, and the early-out behaviour on a disposed instance.
/// </summary>
public sealed class BasicTcpClientTransportTests
{
    [Fact]
    public void Constructor_with_valid_arguments_sets_IsConnected_false()
    {
        using var sut = new BasicTcpClientTransport("127.0.0.1", 1710);
        sut.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Constructor_rejects_null_or_empty_hostname()
    {
        Action nullHost = () => _ = new BasicTcpClientTransport(null!, 1710);
        Action emptyHost = () => _ = new BasicTcpClientTransport(string.Empty, 1710);

        nullHost.Should().Throw<ArgumentNullException>();
        emptyHost.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_rejects_out_of_range_port()
    {
        Action negativePort = () => _ = new BasicTcpClientTransport("127.0.0.1", -1);
        Action tooHighPort = () => _ = new BasicTcpClientTransport("127.0.0.1", 65536);

        negativePort.Should().Throw<ArgumentException>();
        tooHighPort.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_rejects_negative_buffer_size()
    {
        Action act = () => _ = new BasicTcpClientTransport("127.0.0.1", 1710, bufferSize: -1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Send_with_null_payload_throws_ArgumentNullException()
    {
        using var sut = new BasicTcpClientTransport("127.0.0.1", 1710);
        Action act = () => sut.Send(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Send_when_not_connected_throws_InvalidOperationException()
    {
        using var sut = new BasicTcpClientTransport("127.0.0.1", 1710);
        Action act = () => sut.Send(new byte[] { 0x01 });
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Transport is not connected.");
    }

    [Fact]
    public void Disconnect_after_Dispose_is_a_no_op_and_does_not_throw()
    {
        var sut = new BasicTcpClientTransport("127.0.0.1", 1710);
        sut.Dispose();

        Action act = () => sut.Disconnect();
        act.Should().NotThrow();
    }

    [Fact]
    public void Connect_after_Dispose_throws_ObjectDisposedException()
    {
        var sut = new BasicTcpClientTransport("127.0.0.1", 1710);
        sut.Dispose();

        Action act = () => sut.Connect();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Send_after_Dispose_throws_ObjectDisposedException()
    {
        var sut = new BasicTcpClientTransport("127.0.0.1", 1710);
        sut.Dispose();

        Action act = () => sut.Send(new byte[] { 0x01 });
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_is_idempotent()
    {
        var sut = new BasicTcpClientTransport("127.0.0.1", 1710);
        sut.Dispose();

        Action secondDispose = () => sut.Dispose();
        secondDispose.Should().NotThrow();
    }
}
