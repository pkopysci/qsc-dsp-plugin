// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QscDspDevices.TestSupport.Fakes;

/// <summary>
/// In-process TCP server that speaks the QSC ECP protocol per
/// <c>research/ECP_PROTOCOL.md</c>. Used by integration tests to
/// exercise the QscDspDevices ECP backend without a real Q-SYS Core.
/// </summary>
/// <remarks>
/// Reads <c>\n</c>-terminated commands, replies with <c>\r\n</c>-terminated
/// responses. Default behaviour is anonymous (no <c>login_required</c>
/// banner); call <see cref="RequireLogin"/> to flip it. Tracks every
/// received command for assertions.
/// </remarks>
public sealed class FakeEcpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<int, TcpClient> _clients = new();
    private readonly object _stateLock = new();
    private readonly List<string> _receivedCommands = new();

    private Task? _acceptLoop;
    private string? _designName = "TestDesign";
    private string? _designId = "TestId";
    private int _isPrimary = 1;
    private int _isActive = 1;
    private bool _coreNotActive;
    private bool _emitMalformedNext;
    private string? _requiredLoginName;
    private string? _requiredLoginPin;
    private int _nextClientId;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeEcpServer"/>
    /// class. Binds to localhost on a random port and starts accepting.
    /// </summary>
    public FakeEcpServer()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    /// <summary>Gets the TCP port the server is listening on.</summary>
    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>
    /// Sets the design name / id reported by <c>sg</c>. Useful for
    /// asserting design-id changes in reconnect scenarios.
    /// </summary>
    /// <param name="name">The design name.</param>
    /// <param name="id">The design id.</param>
    public void SetDesign(string name, string id)
    {
        lock (_stateLock)
        {
            _designName = name;
            _designId = id;
        }
    }

    /// <summary>
    /// Toggles the <c>IS_ACTIVE</c> flag in subsequent <c>sr</c>
    /// responses. Used to simulate Standby/Active transitions for the
    /// redundancy-via-sg-poll path.
    /// </summary>
    /// <param name="isActive">True for Active, false for Standby.</param>
    public void SetActive(bool isActive)
    {
        lock (_stateLock)
        {
            _isActive = isActive ? 1 : 0;
        }
    }

    /// <summary>Fault-injection: every write reply with <c>core_not_active</c>.</summary>
    /// <param name="enabled">Whether the fault is active.</param>
    public void RespondWithCoreNotActive(bool enabled = true)
    {
        lock (_stateLock)
        {
            _coreNotActive = enabled;
        }
    }

    /// <summary>Fault-injection: the next reply omits the trailing newline.</summary>
    public void EmitMalformed()
    {
        lock (_stateLock)
        {
            _emitMalformedNext = true;
        }
    }

    /// <summary>
    /// Configures the server to send <c>login_required</c> immediately on
    /// accept and accept only the supplied credentials. Other login attempts
    /// receive <c>login_failed</c> and the socket is closed.
    /// </summary>
    /// <param name="name">The accepted user name.</param>
    /// <param name="pin">The accepted PIN.</param>
    public void RequireLogin(string name, string pin)
    {
        lock (_stateLock)
        {
            _requiredLoginName = name;
            _requiredLoginPin = pin;
        }
    }

    /// <summary>Returns a snapshot of every command received so far.</summary>
    /// <returns>The commands in receive order.</returns>
    public IReadOnlyList<string> GetReceivedCommands()
    {
        lock (_stateLock)
        {
            return _receivedCommands.ToArray();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _listener.Stop();
            _listener.Dispose();
        }
        catch (SocketException)
        {
            // Listener already torn down.
        }

        foreach (TcpClient client in _clients.Values)
        {
            try
            {
                client.Close();
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException or InvalidOperationException)
            {
                // Best-effort close.
            }
        }

        try
        {
            _acceptLoop?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Shutdown exceptions are expected.
        }

        _cts.Dispose();
    }

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

            int id = Interlocked.Increment(ref _nextClientId);
            _clients[id] = client;
            _ = Task.Run(() => HandleClientAsync(id, client, cancellationToken), CancellationToken.None);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Test fake; client-handler must not crash the test runner on any peer behaviour. Swallow + log.")]
    private async Task HandleClientAsync(int id, TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using NetworkStream stream = client.GetStream();
            stream.ReadTimeout = 60000;

            // login_required banner if configured.
            string? requiredName;
            string? requiredPin;
            lock (_stateLock)
            {
                requiredName = _requiredLoginName;
                requiredPin = _requiredLoginPin;
            }

            bool authed = requiredName is null;
            if (!authed)
            {
                await WriteAsync(stream, "login_required").ConfigureAwait(false);
            }

            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (IOException)
                {
                    break;
                }

                if (line is null)
                {
                    break;
                }

                lock (_stateLock)
                {
                    _receivedCommands.Add(line);
                }

                if (!authed)
                {
                    if (line.StartsWith("login ", StringComparison.Ordinal))
                    {
                        if (TryAuth(line, requiredName!, requiredPin!))
                        {
                            authed = true;
                            await WriteAsync(stream, "login_success").ConfigureAwait(false);
                        }
                        else
                        {
                            await WriteAsync(stream, "login_failed").ConfigureAwait(false);
                            return;
                        }
                    }
                    else
                    {
                        await WriteAsync(stream, "login_required").ConfigureAwait(false);
                    }

                    continue;
                }

                await DispatchCommandAsync(stream, line).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException or SocketException or ObjectDisposedException)
        {
            // Best-effort: swallow any client-handler failure.
        }
        finally
        {
            _clients.TryRemove(id, out _);
            try
            {
                client.Close();
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException or InvalidOperationException)
            {
                // Best-effort close.
            }
        }
    }

    private async Task DispatchCommandAsync(NetworkStream stream, string line)
    {
        string verb = line.Split(' ', 2)[0];

        bool coreNotActive;
        lock (_stateLock)
        {
            coreNotActive = _coreNotActive;
        }

        if (coreNotActive && verb is not ("sg" or "ct"))
        {
            await WriteAsync(stream, "core_not_active").ConfigureAwait(false);
            return;
        }

        switch (verb)
        {
            case "sg":
                await EmitStatusReportAsync(stream).ConfigureAwait(false);
                break;
            case "csv":
            case "css":
            case "csp":
                await EmitControlEchoAsync(stream, line).ConfigureAwait(false);
                break;
            case "cg":
                await EmitControlEchoAsync(stream, line).ConfigureAwait(false);
                break;
            case "ct":
                // Trigger: no response per ECP §3.3.
                break;
            case "ssl":
                // Snapshot Load: no response per ECP §3.6.
                break;
            case "cgc":
            case "cga":
            case "cgsna":
            case "cgd":
                // Change-group commands: ack via cgpa for the cgs/cgsna
                // variants we exercise; cgc/cga/cgd are no-response per
                // the spec.
                break;
            default:
                // Unknown verb: silent — real Core would emit bad_command.
                break;
        }
    }

    private Task EmitStatusReportAsync(NetworkStream stream)
    {
        string designName;
        string designId;
        int isPrimary;
        int isActive;
        lock (_stateLock)
        {
            designName = _designName ?? string.Empty;
            designId = _designId ?? string.Empty;
            isPrimary = _isPrimary;
            isActive = _isActive;
        }

        string sr = $"sr \"{designName}\" \"{designId}\" {isPrimary.ToString(CultureInfo.InvariantCulture)} {isActive.ToString(CultureInfo.InvariantCulture)}";
        return WriteAsync(stream, sr);
    }

    private Task EmitControlEchoAsync(NetworkStream stream, string command)
    {
        // Quick-and-dirty echo: extract the control id and emit a cv
        // line with the supplied value (or a fixed default for cg).
        string[] args = SplitArgs(command);
        if (args.Length < 2)
        {
            return Task.CompletedTask;
        }

        string id = args[1];
        double value = 0;
        if (args[0] != "cg" && args.Length >= 3 && double.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            value = parsed;
        }

        string display = value.ToString(CultureInfo.InvariantCulture);
        string cv = $"cv \"{id}\" \"{display}\" {value.ToString(CultureInfo.InvariantCulture)} 0";
        return WriteAsync(stream, cv);
    }

    private async Task WriteAsync(NetworkStream stream, string line)
    {
        bool malformed;
        lock (_stateLock)
        {
            malformed = _emitMalformedNext;
            _emitMalformedNext = false;
        }

        string text = malformed ? line : line + "\r\n";
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        await stream.WriteAsync(bytes).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
    }

#pragma warning disable SA1204 // Static members should appear before non-static members
    private static bool TryAuth(string line, string requiredName, string requiredPin)
    {
        string[] parts = SplitArgs(line);
        if (parts.Length < 3)
        {
            return false;
        }

        return string.Equals(parts[1], requiredName, StringComparison.Ordinal)
            && string.Equals(parts[2], requiredPin, StringComparison.Ordinal);
    }

    private static string[] SplitArgs(string command)
    {
        // Tokenize honouring quotes. Quotes are stripped from the tokens.
        var tokens = new List<string>();
        int i = 0;
        while (i < command.Length)
        {
            char ch = command[i];
            if (char.IsWhiteSpace(ch))
            {
                i++;
                continue;
            }

            if (ch == '"')
            {
                int end = command.IndexOf('"', i + 1);
                if (end < 0)
                {
                    tokens.Add(command[(i + 1)..]);
                    return tokens.ToArray();
                }

                tokens.Add(command[(i + 1)..end]);
                i = end + 1;
            }
            else
            {
                int end = i;
                while (end < command.Length && !char.IsWhiteSpace(command[end]))
                {
                    end++;
                }

                tokens.Add(command[i..end]);
                i = end;
            }
        }

        return tokens.ToArray();
    }
#pragma warning restore SA1204
}
