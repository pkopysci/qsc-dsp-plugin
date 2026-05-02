// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using QscDspDevices.AudioControl;
using QscDspDevices.Connectivity;
using QscDspDevices.Connectivity.PostConnect;
using QscDspDevices.LogicTriggers;
using QscDspDevices.Plugin.Threading;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.TestSupport.Fakes;
using QscDspDevices.TestSupport.Transport;
using Xunit;

namespace QscDspDevices.IntegrationTests.Audio;

/// <summary>
/// End-to-end tests for the M5 logic-trigger surface against the
/// in-process FakeQrcServer.
/// </summary>
public sealed class LogicTriggersEndToEndTests
{
    private static readonly IReadOnlyList<string> NoTags = Array.Empty<string>();

    [Fact]
    public async Task PulseDspLogicTrigger_round_trips_via_Control_Set_true()
    {
        using var env = new IntegrationEnv();
        env.Manager.Connect();
        await WaitForStateAsync(env.Manager, ConnectionState.Connected);

        // Hydration: 1 input (lvl + mute) + 1 trigger + 1 AutoPoll = 4 frames.
        await WaitForFrameCountAsync(env.Server, expected: 4);

        env.Trigger.Pulse("rec");

        await WaitForFrameCountAsync(env.Server, expected: 5);

        IReadOnlyList<ReceivedFrame> frames = env.Server.GetReceivedFrames();
        ReceivedFrame pulse = frames[^1];
        pulse.Method.Should().Be("Control.Set");
        var p = (JObject)pulse.Params!;
        p["Name"]!.ToString().Should().Be("rec.start");
        p["Value"]!.ToObject<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task Server_pushed_AutoPoll_on_triggerTag_fires_DspLogicTriggerStateChanged()
    {
        using var env = new IntegrationEnv();
        env.Manager.Connect();
        await WaitForStateAsync(env.Manager, ConnectionState.Connected);
        await WaitForFrameCountAsync(env.Server, expected: 4);

        var raised = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        env.Trigger.LogicTriggerStateChanged += (_, args) => raised.TrySetResult(args.Arg);

        await env.Server.PushAutoPollDeltaAsync(
            ChangeGroupManager.PluginGroupId,
            new (string, object)[] { ("rec.start", true) });

        string triggerId = await raised.Task.WaitAsync(TimeSpan.FromSeconds(5));
        triggerId.Should().Be("rec");
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

    private sealed class IntegrationEnv : IDisposable
    {
        public IntegrationEnv()
        {
            Server = new FakeQrcServer();
            Transport = new RawTcpTransport("127.0.0.1", Server.Port);
            Queue = new CommandQueue("dsp-1");
            Dispatcher = new JsonRpcDispatcher("dsp-1");

            Channels = new AudioChannelRegistry("dsp-1");
            Channels.RegisterInput(new AudioChannel("mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 1, NoTags));

            ZoneRegistry = new AudioZoneRegistry("dsp-1");
            TriggerRegistry = new LogicTriggerRegistry("dsp-1");
            TriggerRegistry.Register("rec", "rec.start");

            var scaler = new LevelScaler("dsp-1");
            var ids = new IdGenerator();
            var groupManager = new ChangeGroupManager("dsp-1", ids);
            var audio = new AudioControlService("dsp-1", Channels, scaler, Queue, ids);
            var routing = new AudioRoutingService("dsp-1", Channels, Queue, ids);
            var zone = new AudioZoneEnableService("dsp-1", ZoneRegistry, Queue, ids);
            Trigger = new LogicTriggerService("dsp-1", TriggerRegistry, Queue, ids);

            var fanout = new AudioControlServiceFanout(
                Channels, ZoneRegistry, TriggerRegistry, routing, zone, Trigger, audio);
            groupManager.SetDeltaCallback(fanout.Dispatch);

            var hydrate = new HydrateChangeGroupAction(
                "dsp-1", Channels, ZoneRegistry, TriggerRegistry, groupManager, Queue, Dispatcher, logon: null);

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

        public AudioChannelRegistry Channels
        {
            get;
        }

        public AudioZoneRegistry ZoneRegistry
        {
            get;
        }

        public LogicTriggerRegistry TriggerRegistry
        {
            get;
        }

        public LogicTriggerService Trigger
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
