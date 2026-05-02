// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using QscDspDevices.AudioControl;
using QscDspDevices.Connectivity;
using QscDspDevices.Connectivity.PostConnect;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.TestSupport.Fakes;
using QscDspDevices.TestSupport.Transport;
using Xunit;

namespace QscDspDevices.IntegrationTests.Audio;

/// <summary>
/// End-to-end audio-control tests against the in-process FakeQrcServer.
/// Wires the full M3 stack (registry, scaler, services, change-group
/// manager, post-connect actions, connection manager, raw-TCP transport)
/// and asserts on the wire-level frames the server received.
/// </summary>
public sealed class AudioControlEndToEndTests
{
    private static readonly IReadOnlyList<string> NoTags = Array.Empty<string>();

    [Fact]
    public async Task Connect_with_credentials_sends_Logon_then_subscribes()
    {
        using var env = new IntegrationEnv(includeChannels: true, username: "alice", password: "p4ss");
        env.Server.RequireLogonPin("p4ss");

        env.Manager.Connect();
        await WaitForStateAsync(env.Manager, ConnectionState.Connected);
        await WaitForFrameCountAsync(env.Server, expected: 4);

        IReadOnlyList<ReceivedFrame> received = env.Server.GetReceivedFrames();
        received[0].Method.Should().Be("Logon");
        received.Skip(1).Take(2).Should().AllSatisfy(f => f.Method.Should().Be("ChangeGroup.AddControl"));
        received[3].Method.Should().Be("ChangeGroup.AutoPoll");
    }

    [Fact]
    public async Task SetAudioInputLevel_round_trips_via_Control_Set_on_the_wire()
    {
        using var env = new IntegrationEnv(includeChannels: true);
        env.Manager.Connect();
        await WaitForStateAsync(env.Manager, ConnectionState.Connected);
        await WaitForFrameCountAsync(env.Server, expected: 3); // 2 AddControl + 1 AutoPoll

        env.Audio.SetLevel("mic1", 50);

        await WaitForFrameCountAsync(env.Server, expected: 4);

        IReadOnlyList<ReceivedFrame> framesAfterSet = env.Server.GetReceivedFrames();
        ReceivedFrame controlSet = framesAfterSet[^1];
        controlSet.Method.Should().Be("Control.Set");
        var p = (JObject)controlSet.Params!;
        p["Name"]!.ToString().Should().Be("mic1.gain");
        p["Value"]!.ToObject<double>().Should().BeApproximately(-40.0, 1e-9);
    }

    [Fact]
    public async Task RecallAudioPreset_sends_Snapshot_Load_with_correct_bank_and_index()
    {
        using var env = new IntegrationEnv(includeChannels: false);
        env.Registry.RegisterPreset(new AudioPreset("dinner", "MainBank", 3));

        env.Manager.Connect();
        await WaitForStateAsync(env.Manager, ConnectionState.Connected);

        env.Preset.Recall("dinner");

        await WaitForFrameCountAsync(env.Server, expected: 1);

        IReadOnlyList<ReceivedFrame> framesAfterRecall = env.Server.GetReceivedFrames();
        ReceivedFrame snap = framesAfterRecall[0];
        snap.Method.Should().Be("Snapshot.Load");
        var p = (JObject)snap.Params!;
        p["Name"]!.ToString().Should().Be("MainBank");
        p["Bank"]!.ToObject<int>().Should().Be(3);
        p.ContainsKey("Ramp").Should().BeFalse();
    }

    [Fact]
    public async Task Server_pushed_AutoPoll_delta_fires_AudioInputMuteChanged()
    {
        using var env = new IntegrationEnv(includeChannels: true);
        env.Manager.Connect();
        await WaitForStateAsync(env.Manager, ConnectionState.Connected);
        await WaitForFrameCountAsync(env.Server, expected: 3);

        var raised = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        env.Audio.AudioInputMuteChanged += (_, args) => raised.TrySetResult(args.Arg2);

        await env.Server.PushAutoPollDeltaAsync(
            ChangeGroupManager.PluginGroupId,
            new (string, object)[] { ("mic1.mute", true) });

        string raisedChannelId = await raised.Task.WaitAsync(TimeSpan.FromSeconds(5));

        raisedChannelId.Should().Be("mic1");
        env.Audio.GetMute("mic1").Should().BeTrue();
    }

    // Note on Reconnect_re_subscribes_change_group_with_same_controls:
    // The "reconnect rebuilds group" requirement is already implied by
    //   - ConnectionManagerTests.Mid_flight_drop_triggers_reconnect_loop
    //     (verifies reconnect happens via deterministic clock)
    //   - HydrateChangeGroupActionTests.Hydration_enqueues_AddControl_per_tag_then_AutoPoll
    //     (verifies hydration produces the correct subscribe shape)
    // An end-to-end real-clock variant would need the 15s reconnect wait
    // and the FakeQrcServer's listener to accept a fresh connection from
    // the same client — both heavy on the CI runner. Re-evaluate in M7
    // hardening if a regression here is observed.
    private static async Task WaitForStateAsync(ConnectionManager manager, ConnectionState desired, TimeSpan? timeout = null)
    {
        TimeSpan deadline = timeout ?? TimeSpan.FromSeconds(5);
        DateTime end = DateTime.UtcNow + deadline;
        while (manager.State != desired)
        {
            if (DateTime.UtcNow > end)
            {
                throw new TimeoutException($"State did not reach {desired} within {deadline.TotalSeconds:0.##}s; current is {manager.State}.");
            }

            await Task.Delay(20);
        }
    }

    private static async Task WaitForFrameCountAsync(FakeQrcServer server, int expected, double timeoutSeconds = 5)
    {
        DateTime end = DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSeconds);
        while (server.GetReceivedFrames().Count < expected)
        {
            if (DateTime.UtcNow > end)
            {
                throw new TimeoutException($"Received {server.GetReceivedFrames().Count} of {expected} frames within {timeoutSeconds:0.##}s.");
            }

            await Task.Delay(20);
        }
    }

    /// <summary>
    /// Composes the full M3 stack — server, transport, queue, dispatcher,
    /// registry, scaler, audio + preset services, change-group manager,
    /// post-connect actions, connection manager — for a single test.
    /// </summary>
    private sealed class IntegrationEnv : IDisposable
    {
        public IntegrationEnv(bool includeChannels, string? username = null, string? password = null)
        {
            Server = new FakeQrcServer();
            Transport = new RawTcpTransport("127.0.0.1", Server.Port);
            Queue = new CommandQueue("dsp-1");
            Dispatcher = new JsonRpcDispatcher("dsp-1");
            Registry = new AudioChannelRegistry("dsp-1");

            if (includeChannels)
            {
                Registry.RegisterInput(new AudioChannel(
                    "mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 0, NoTags));
            }

            var scaler = new LevelScaler("dsp-1");
            var ids = new IdGenerator();
            var groupManager = new ChangeGroupManager("dsp-1", ids);
            Audio = new AudioControlService("dsp-1", Registry, scaler, Queue, ids);
            Preset = new PresetService("dsp-1", Registry, Queue, ids);
            groupManager.SetDeltaCallback(Audio.OnDeviceUpdate);

            LogonAction? logon = (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
                ? null
                : new LogonAction(
                    "dsp-1",
                    () => new LogonCredentials(username, password),
                    Queue,
                    Dispatcher,
                    ids);
            var hydrate = new HydrateChangeGroupAction("dsp-1", Registry, groupManager, Queue, Dispatcher, logon);

            IPostConnectAction post = logon is null
                ? hydrate
                : new CompositePostConnectAction(new IPostConnectAction[] { logon, hydrate });

            Manager = new ConnectionManager(
                "dsp-1", Transport, new ReconnectStrategy(new SystemClock()), Queue, Dispatcher, post);
        }

        public FakeQrcServer Server
        {
            get;
        }

        public RawTcpTransport Transport
        {
            get;
        }

        public CommandQueue Queue
        {
            get;
        }

        public JsonRpcDispatcher Dispatcher
        {
            get;
        }

        public AudioChannelRegistry Registry
        {
            get;
        }

        public AudioControlService Audio
        {
            get;
        }

        public PresetService Preset
        {
            get;
        }

        public ConnectionManager Manager
        {
            get;
        }

        public void Dispose()
        {
            Manager.Dispose();
            Transport.Dispose();
            Queue.Dispose();
            Server.Dispose();
        }
    }
}
