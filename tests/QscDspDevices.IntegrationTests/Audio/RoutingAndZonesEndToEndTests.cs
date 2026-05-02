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
/// End-to-end tests for the M4 routing + zone-enable surfaces against
/// the in-process FakeQrcServer.
/// </summary>
public sealed class RoutingAndZonesEndToEndTests
{
    private static readonly IReadOnlyList<string> NoTags = Array.Empty<string>();

    [Fact]
    public async Task RouteAudio_round_trips_via_Control_Set_on_routerTag()
    {
        using var env = new IntegrationEnv(routedOutputCount: 1);
        env.Manager.Connect();
        await WaitForStateAsync(env.Manager, ConnectionState.Connected);

        // Hydration: 1 input gives 2 tags (lvl, mute); 1 output gives 3
        // (lvl, mute, router); plus 1 AutoPoll. Wait for all 6 frames.
        await WaitForFrameCountAsync(env.Server, expected: 6);

        env.Routing.Route("mic1", "out1");

        await WaitForFrameCountAsync(env.Server, expected: 7);

        IReadOnlyList<ReceivedFrame> frames = env.Server.GetReceivedFrames();
        ReceivedFrame controlSet = frames[^1];
        controlSet.Method.Should().Be("Control.Set");
        var p = (JObject)controlSet.Params!;
        p["Name"]!.ToString().Should().Be("mixer.out1.source");
        p["Value"]!.ToObject<int>().Should().Be(3);
    }

    [Fact]
    public async Task Server_pushed_AutoPoll_on_routerTag_fires_AudioRouteChanged()
    {
        using var env = new IntegrationEnv(routedOutputCount: 1);
        env.Manager.Connect();
        await WaitForStateAsync(env.Manager, ConnectionState.Connected);
        await WaitForFrameCountAsync(env.Server, expected: 6);

        var raised = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        env.Routing.RouteChanged += (_, args) => raised.TrySetResult(args.Arg2);

        await env.Server.PushAutoPollDeltaAsync(
            ChangeGroupManager.PluginGroupId,
            new (string, object)[] { ("mixer.out1.source", 3) });

        string outputId = await raised.Task.WaitAsync(TimeSpan.FromSeconds(5));
        outputId.Should().Be("out1");
        env.Routing.GetCurrentSource("out1").Should().Be("mic1");
    }

    [Fact]
    public async Task SetAudioZoneEnable_round_trips_via_Control_Set_on_zone_controlTag()
    {
        using var env = new IntegrationEnv(routedOutputCount: 0, zonePairs: 1);
        env.Manager.Connect();
        await WaitForStateAsync(env.Manager, ConnectionState.Connected);

        // 1 input (lvl + mute) + 1 zone + 1 AutoPoll = 4 frames.
        await WaitForFrameCountAsync(env.Server, expected: 4);

        env.ZoneEnable.Set("mic1", "zoneA", true);

        await WaitForFrameCountAsync(env.Server, expected: 5);

        IReadOnlyList<ReceivedFrame> frames = env.Server.GetReceivedFrames();
        ReceivedFrame zone = frames[^1];
        zone.Method.Should().Be("Control.Set");
        var p = (JObject)zone.Params!;
        p["Name"]!.ToString().Should().Be("zone.mic1.zoneA.enable");
        p["Value"]!.ToObject<bool>().Should().BeTrue();
    }

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
    /// Composes the full M3+M4 stack so the routing + zone tests share a
    /// single fake server, transport, queue, dispatcher, and post-connect
    /// chain.
    /// </summary>
    private sealed class IntegrationEnv : IDisposable
    {
        public IntegrationEnv(int routedOutputCount, int zonePairs = 0)
        {
            Server = new FakeQrcServer();
            Transport = new RawTcpTransport("127.0.0.1", Server.Port);
            Queue = new CommandQueue("dsp-1");
            Dispatcher = new JsonRpcDispatcher("dsp-1");
            Registry = new AudioChannelRegistry("dsp-1");
            ZoneRegistry = new AudioZoneRegistry("dsp-1");

            // One input registered with a known bankIndex so RouteAudio
            // resolves cleanly.
            Registry.RegisterInput(new AudioChannel(
                "mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 3, NoTags));

            for (int i = 0; i < routedOutputCount; i++)
            {
                Registry.RegisterOutput(new AudioChannel(
                    "out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, "mixer.out1.source"));
            }

            for (int i = 0; i < zonePairs; i++)
            {
                ZoneRegistry.TryRegister("mic1", "zoneA", "zone.mic1.zoneA.enable");
            }

            var scaler = new LevelScaler("dsp-1");
            var ids = new IdGenerator();
            var groupManager = new ChangeGroupManager("dsp-1", ids);
            Audio = new AudioControlService("dsp-1", Registry, scaler, Queue, ids);
            Routing = new AudioRoutingService("dsp-1", Registry, Queue, ids);
            ZoneEnable = new AudioZoneEnableService("dsp-1", ZoneRegistry, Queue, ids);

            var fanout = new AudioControlServiceFanout(Registry, ZoneRegistry, Routing, ZoneEnable, Audio);
            groupManager.SetDeltaCallback(fanout.Dispatch);

            var hydrate = new HydrateChangeGroupAction("dsp-1", Registry, ZoneRegistry, groupManager, Queue, Dispatcher, logon: null);

            Manager = new ConnectionManager(
                "dsp-1", Transport, new ReconnectStrategy(new SystemClock()), Queue, Dispatcher, hydrate);
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

        public AudioZoneRegistry ZoneRegistry
        {
            get;
        }

        public AudioControlService Audio
        {
            get;
        }

        public AudioRoutingService Routing
        {
            get;
        }

        public AudioZoneEnableService ZoneEnable
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
