// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QscDspDevices.TestSupport.Fakes;

/// <summary>
/// In-process TCP server that speaks the QRC protocol per
/// <c>research/QRC_PROTOCOL.md</c>. Used by integration tests to exercise
/// the QscDspDevices client without a real Q-SYS Core.
/// </summary>
/// <remarks>
/// <para>
/// On TCP accept the server immediately writes an <c>EngineStatus</c>
/// notification (matching real Core behaviour, §15.1 of the QRC research).
/// It then reads null-byte-framed JSON-RPC requests and dispatches them
/// to the appropriate built-in handler (<c>NoOp</c>, <c>Logon</c>,
/// <c>Component.Set/Get</c>, <c>Control.Set/Get</c>, <c>ChangeGroup.*</c>,
/// <c>Snapshot.Load</c>, <c>StatusGet</c>). Unknown methods return
/// JSON-RPC error <c>-32601</c>.
/// </para>
/// <para>
/// Failure-injection knobs let tests force every interesting failure
/// mode without standing up a real broken peer:
/// <list type="bullet">
///   <item><see cref="DropConnection"/> — closes every active client socket.</item>
///   <item><see cref="DelayResponseMs"/> — every response is delayed by N ms.</item>
///   <item><see cref="EmitMalformed"/> — the next response is malformed JSON.</item>
///   <item><see cref="RespondWithStandbyError"/> — every method returns -32604.</item>
///   <item><see cref="RequireLogonPin"/> — non-Logon/NoOp requests return error 10 until Logon succeeds.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class FakeQrcServer : IDisposable
{
    private const byte FrameTerminator = 0x00;

    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<int, ClientSession> _clients = new();
    private readonly object _failureLock = new();

    private Task? _acceptLoop;
    private int _delayMs;
    private bool _emitMalformedNext;
    private bool _respondWithStandby;
    private string? _requiredLogonPin;
    private int _receivedFrames;
    private int _nextClientId;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeQrcServer"/> class
    /// bound to a free local TCP port.
    /// </summary>
    public FakeQrcServer()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    /// <summary>Gets the loopback TCP port the server is listening on.</summary>
    public int Port
    {
        get;
    }

    /// <summary>Gets the count of frames the server has received across all clients.</summary>
    public int ReceivedFrameCount => Volatile.Read(ref _receivedFrames);

    /// <summary>Gets the count of currently-connected clients.</summary>
    public int ConnectedClientCount => _clients.Count;

    /// <summary>
    /// Configures the server to require a Logon with the supplied PIN
    /// before accepting any other method. Pass <c>null</c> to disable.
    /// </summary>
    /// <param name="pin">The required PIN, or null to disable.</param>
    public void RequireLogonPin(string? pin)
    {
        lock (_failureLock)
        {
            _requiredLogonPin = pin;
        }
    }

    /// <summary>Closes every active client connection, simulating a network drop.</summary>
    public void DropConnection()
    {
        foreach ((int _, ClientSession client) in _clients)
        {
            client.Disconnect();
        }
    }

    /// <summary>
    /// Sets a per-response artificial delay. Default zero. Used to test
    /// in-flight cancellation and timeout behaviour.
    /// </summary>
    /// <param name="ms">Milliseconds to delay every response.</param>
    public void DelayResponseMs(int ms)
    {
        lock (_failureLock)
        {
            _delayMs = ms;
        }
    }

    /// <summary>
    /// Causes the NEXT response to be a malformed JSON frame, after which
    /// normal behaviour resumes. Tests use this to verify the framer + dispatcher
    /// log Error and continue.
    /// </summary>
    public void EmitMalformed()
    {
        lock (_failureLock)
        {
            _emitMalformedNext = true;
        }
    }

    /// <summary>
    /// When enabled, every method response carries error <c>-32604</c>
    /// (CoreOnStandby). Reset by passing <c>false</c>.
    /// </summary>
    /// <param name="enabled">Whether Standby mode is active.</param>
    public void RespondWithStandbyError(bool enabled = true)
    {
        lock (_failureLock)
        {
            _respondWithStandby = enabled;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();

        try
        {
            _listener.Stop();
        }
        catch (SocketException)
        {
            // Listener already torn down.
        }

        _listener.Dispose();

        foreach ((int _, ClientSession client) in _clients)
        {
            client.Disconnect();
        }

        try
        {
            _acceptLoop?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Cancellation noise on shutdown.
        }
        catch (TaskCanceledException)
        {
            // Cancellation noise on shutdown.
        }

        _cts.Dispose();
    }

    internal void OnFrameReceived() => Interlocked.Increment(ref _receivedFrames);

    internal (int DelayMs, bool MalformedNow, bool Standby) SnapshotFailure()
    {
        lock (_failureLock)
        {
            bool malformedNow = _emitMalformedNext;
            _emitMalformedNext = false;
            return (_delayMs, malformedNow, _respondWithStandby);
        }
    }

    internal string? GetRequiredLogonPin()
    {
        lock (_failureLock)
        {
            return _requiredLogonPin;
        }
    }

    internal void OnSessionEnded(int clientId) => _clients.TryRemove(clientId, out _);

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (SocketException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            int clientId = Interlocked.Increment(ref _nextClientId);
            var session = new ClientSession(clientId, client, this, cancellationToken);
            _clients[clientId] = session;
            _ = Task.Run(session.RunAsync, CancellationToken.None);
        }
    }

    /// <summary>One per-connection session that handles framing and method dispatch.</summary>
    private sealed class ClientSession
    {
        private readonly int _id;
        private readonly TcpClient _client;
        private readonly FakeQrcServer _server;
        private readonly CancellationToken _serverCancellation;
        private readonly NetworkStream _stream;

        public ClientSession(int id, TcpClient client, FakeQrcServer server, CancellationToken serverCancellation)
        {
            _id = id;
            _client = client;
            _server = server;
            _serverCancellation = serverCancellation;
            _stream = client.GetStream();
        }

        public bool IsLoggedOn
        {
            get; private set;
        }

        public async Task RunAsync()
        {
            try
            {
                await SendNotificationAsync("EngineStatus", new
                {
                    State = "Active",
                    DesignName = "FakeDesign",
                    DesignCode = "fake-code-001",
                    IsRedundant = false,
                    IsEmulator = true,
                }).ConfigureAwait(false);

                byte[] buffer = new byte[16 * 1024];
                using var pending = new MemoryStream();
                while (!_serverCancellation.IsCancellationRequested)
                {
                    int n;
                    try
                    {
                        n = await _stream.ReadAsync(buffer.AsMemory(), _serverCancellation).ConfigureAwait(false);
                    }
                    catch (IOException)
                    {
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }

                    if (n <= 0)
                    {
                        return;
                    }

                    for (int i = 0; i < n; i++)
                    {
                        if (buffer[i] == FrameTerminator)
                        {
                            byte[] frame = pending.ToArray();
                            pending.SetLength(0);
                            string json = Encoding.UTF8.GetString(frame);
                            await HandleFrameAsync(json).ConfigureAwait(false);
                        }
                        else
                        {
                            pending.WriteByte(buffer[i]);
                        }
                    }
                }
            }
            finally
            {
                Disconnect();
                _server.OnSessionEnded(_id);
            }
        }

        public void Disconnect()
        {
            try
            {
                _stream.Close();
            }
            catch (IOException)
            {
                // Stream already closed.
            }
            catch (ObjectDisposedException)
            {
                // Already disposed.
            }

            try
            {
                _client.Close();
            }
            catch (IOException)
            {
                // Already closed.
            }
            catch (ObjectDisposedException)
            {
                // Already disposed.
            }
        }

        private async Task HandleFrameAsync(string json)
        {
            _server.OnFrameReceived();

            JObject? request;
            try
            {
                request = JObject.Parse(json);
            }
            catch (JsonException)
            {
                return;
            }

            long? id = request.Value<long?>("id");
            string method = request.Value<string?>("method") ?? string.Empty;
            JToken? @params = request["params"];

            (int DelayMs, bool MalformedNow, bool Standby) failure = _server.SnapshotFailure();
            if (failure.DelayMs > 0)
            {
                try
                {
                    await Task.Delay(failure.DelayMs, _serverCancellation).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            if (failure.MalformedNow && id.HasValue)
            {
                await SendRawAsync("{this is not valid json").ConfigureAwait(false);
                return;
            }

            if (!id.HasValue)
            {
                return;
            }

            if (_server.GetRequiredLogonPin() is not null && !IsLoggedOn && method != "Logon" && method != "NoOp")
            {
                await SendErrorAsync(id.Value, 10, "Logon required").ConfigureAwait(false);
                return;
            }

            if (failure.Standby && method != "NoOp")
            {
                await SendErrorAsync(id.Value, -32604, "Core on Standby").ConfigureAwait(false);
                return;
            }

            switch (method)
            {
                case "NoOp":
                    await SendResultAsync(id.Value, true).ConfigureAwait(false);
                    return;

                case "Logon":
                    await HandleLogonAsync(id.Value, @params).ConfigureAwait(false);
                    return;

                case "StatusGet":
                    await SendResultAsync(id.Value, new
                    {
                        Platform = "FakeQrcServer",
                        Version = "0.0.0-test",
                        DesignName = "FakeDesign",
                        DesignCode = "fake-code-001",
                        IsRedundant = false,
                        IsEmulator = true,
                        State = "Active",
                    }).ConfigureAwait(false);
                    return;

                case "Component.Set":
                case "Control.Set":
                case "Snapshot.Load":
                case "Snapshot.Save":
                    await SendResultAsync(id.Value, true).ConfigureAwait(false);
                    return;

                case "Component.Get":
                case "Control.Get":
                    await SendResultAsync(id.Value, new
                    {
                        Name = "stub",
                        Value = 0
                    }).ConfigureAwait(false);
                    return;

                case "ChangeGroup.AddControl":
                case "ChangeGroup.AddComponentControl":
                case "ChangeGroup.Remove":
                case "ChangeGroup.Clear":
                case "ChangeGroup.Destroy":
                case "ChangeGroup.Poll":
                case "ChangeGroup.AutoPoll":
                    await SendResultAsync(id.Value, new
                    {
                        Changes = Array.Empty<object>()
                    }).ConfigureAwait(false);
                    return;

                default:
                    await SendErrorAsync(id.Value, -32601, $"Method '{method}' not found").ConfigureAwait(false);
                    return;
            }
        }

        private async Task HandleLogonAsync(long id, JToken? @params)
        {
            string? receivedPin = @params?.Value<string?>("Password") ?? @params?.Value<string?>("Pin");

            if (_server.GetRequiredLogonPin() is { } expected && receivedPin != expected)
            {
                await SendErrorAsync(id, 10, "Invalid credentials").ConfigureAwait(false);
                return;
            }

            IsLoggedOn = true;
            await SendResultAsync(id, true).ConfigureAwait(false);
        }

        private Task SendResultAsync(long id, object result)
        {
            string json = JsonConvert.SerializeObject(new
            {
                jsonrpc = "2.0",
                id,
                result,
            });
            return SendRawAsync(json);
        }

        private Task SendErrorAsync(long id, int code, string message)
        {
            string json = JsonConvert.SerializeObject(new
            {
                jsonrpc = "2.0",
                id,
                error = new
                {
                    code,
                    message
                },
            });
            return SendRawAsync(json);
        }

        private Task SendNotificationAsync(string method, object @params)
        {
            string json = JsonConvert.SerializeObject(new
            {
                jsonrpc = "2.0",
                method,
                @params,
            });
            return SendRawAsync(json);
        }

        private async Task SendRawAsync(string json)
        {
            byte[] payload = Encoding.UTF8.GetBytes(json);
            try
            {
                await _stream.WriteAsync(payload.AsMemory(), _serverCancellation).ConfigureAwait(false);
                await _stream.WriteAsync(new byte[] { FrameTerminator }.AsMemory(), _serverCancellation).ConfigureAwait(false);
                await _stream.FlushAsync(_serverCancellation).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // Client gone.
            }
            catch (ObjectDisposedException)
            {
                // Client gone.
            }
        }
    }
}
