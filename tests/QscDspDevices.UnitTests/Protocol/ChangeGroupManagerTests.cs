// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.ChangeGroup;
using QscDspDevices.Protocol.JsonRpc;
using Xunit;

namespace QscDspDevices.UnitTests.Protocol;

/// <summary>
/// Unit tests for <see cref="ChangeGroupManager"/>. The manager is
/// pure: it builds JSON-RPC requests and parses AutoPoll pushes; it
/// does not perform any I/O. Tests assert on the outgoing request
/// shape and the inbound delta dispatch behaviour.
/// </summary>
public sealed class ChangeGroupManagerTests
{
    private static readonly string[] ExpectedAB = { "a", "b" };

    [Fact]
    public void BuildAddControl_emits_a_well_formed_AddControl_request()
    {
        var sut = new ChangeGroupManager("dsp-1", new IdGenerator());
        JsonRpcRequest? req = sut.BuildAddControl(ChangeGroupManager.PluginGroupId, "mic1.gain");

        req.Should().NotBeNull();
        req!.Method.Should().Be("ChangeGroup.AddControl");
        req.Id.Should().BeGreaterThan(0);
        var p = JObject.FromObject(req.Params!);
        p["Id"]!.ToString().Should().Be(ChangeGroupManager.PluginGroupId);
        ((JArray)p["Controls"]!).Select(t => t.ToString()).Should().Equal("mic1.gain");
    }

    [Fact]
    public void BuildAutoPoll_emits_a_well_formed_AutoPoll_request_with_default_rate()
    {
        var sut = new ChangeGroupManager("dsp-1", new IdGenerator());
        sut.BuildAddControl(ChangeGroupManager.PluginGroupId, "mic1.gain");

        JsonRpcRequest req = sut.BuildAutoPoll(ChangeGroupManager.PluginGroupId);

        req.Method.Should().Be("ChangeGroup.AutoPoll");
        var p = JObject.FromObject(req.Params!);
        p["Id"]!.ToString().Should().Be(ChangeGroupManager.PluginGroupId);
        p["Rate"]!.ToObject<double>().Should().BeApproximately(0.25, 1e-9);
    }

    [Fact]
    public void BuildAutoPoll_with_no_subscribed_controls_throws()
    {
        var sut = new ChangeGroupManager("dsp-1", new IdGenerator());
        Action act = () => sut.BuildAutoPoll(ChangeGroupManager.PluginGroupId);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void BuildAutoPoll_with_zero_or_negative_rate_throws()
    {
        var sut = new ChangeGroupManager("dsp-1", new IdGenerator());
        sut.BuildAddControl(ChangeGroupManager.PluginGroupId, "x");

        Action zero = () => sut.BuildAutoPoll(ChangeGroupManager.PluginGroupId, 0);
        Action neg = () => sut.BuildAutoPoll(ChangeGroupManager.PluginGroupId, -0.1);
        zero.Should().Throw<ArgumentOutOfRangeException>();
        neg.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void BuildDestroy_emits_a_Destroy_request_and_clears_local_state()
    {
        var sut = new ChangeGroupManager("dsp-1", new IdGenerator());
        sut.BuildAddControl(ChangeGroupManager.PluginGroupId, "x");
        sut.GroupCount.Should().Be(1);

        JsonRpcRequest? req = sut.BuildDestroy(ChangeGroupManager.PluginGroupId);

        req.Should().NotBeNull();
        req!.Method.Should().Be("ChangeGroup.Destroy");
        sut.GroupCount.Should().Be(0);
    }

    [Fact]
    public void BuildDestroy_for_unknown_group_returns_null()
    {
        var sut = new ChangeGroupManager("dsp-1", new IdGenerator());
        sut.BuildDestroy("never-created").Should().BeNull();
    }

    [Fact]
    public void Adding_a_fifth_distinct_group_is_refused_at_the_QRC_protocol_cap()
    {
        var sut = new ChangeGroupManager("dsp-1", new IdGenerator());
        for (int i = 0; i < ChangeGroupManager.MaxGroupsPerConnection; i++)
        {
            sut.BuildAddControl($"group-{i}", "x").Should().NotBeNull();
        }

        sut.BuildAddControl("group-extra", "x").Should().BeNull();
        sut.GroupCount.Should().Be(ChangeGroupManager.MaxGroupsPerConnection);
    }

    [Fact]
    public void OnPush_with_two_deltas_invokes_the_callback_twice()
    {
        var sut = new ChangeGroupManager("dsp-1", new IdGenerator());
        var collected = new List<ChangeGroupDelta>();
        sut.SetDeltaCallback(collected.Add);

        var response = new JsonRpcResponse
        {
            Id = 1,
            Result = JToken.Parse(@"{
                ""Id"": ""qsc-plugin-state"",
                ""Changes"": [
                  { ""Name"": ""mic1.gain"", ""Value"": -40.5, ""Position"": 0.5 },
                  { ""Name"": ""mic1.mute"", ""Value"": true }
                ]
              }"),
        };

        sut.OnPush(response);

        collected.Should().HaveCount(2);
        collected[0].Name.Should().Be("mic1.gain");
        collected[0].Position.Should().BeApproximately(0.5, 1e-9);
        collected[1].Name.Should().Be("mic1.mute");
        collected[1].Value.ToObject<bool>().Should().BeTrue();
    }

    [Fact]
    public void OnPush_with_an_error_response_logs_and_skips_the_callback()
    {
        var sut = new ChangeGroupManager("dsp-1", new IdGenerator());
        bool called = false;
        sut.SetDeltaCallback(_ => called = true);

        var errorResponse = new JsonRpcResponse
        {
            Id = 1,
            Error = new JsonRpcError { Code = -32604, Message = "Core on Standby" },
        };

        Action act = () => sut.OnPush(errorResponse);
        act.Should().NotThrow();
        called.Should().BeFalse();
    }

    [Fact]
    public void OnPush_with_no_callback_set_is_a_no_op()
    {
        var sut = new ChangeGroupManager("dsp-1", new IdGenerator());
        var response = new JsonRpcResponse
        {
            Id = 1,
            Result = JToken.Parse(@"{ ""Changes"": [{ ""Name"": ""x"", ""Value"": 1 }] }"),
        };

        Action act = () => sut.OnPush(response);
        act.Should().NotThrow();
    }

    [Fact]
    public void OnPush_with_malformed_or_missing_Changes_skips_silently()
    {
        var sut = new ChangeGroupManager("dsp-1", new IdGenerator());
        bool called = false;
        sut.SetDeltaCallback(_ => called = true);

        var noResult = new JsonRpcResponse { Id = 1 };
        var noChanges = new JsonRpcResponse { Id = 1, Result = JToken.Parse("{}") };
        var changesNotArray = new JsonRpcResponse { Id = 1, Result = JToken.Parse(@"{""Changes"": 5}") };
        var entryMissingName = new JsonRpcResponse
        {
            Id = 1,
            Result = JToken.Parse(@"{""Changes"":[{""Value"": 1}]}"),
        };

        sut.OnPush(noResult);
        sut.OnPush(noChanges);
        sut.OnPush(changesNotArray);
        sut.OnPush(entryMissingName);

        called.Should().BeFalse();
    }

    [Fact]
    public void Constructor_with_null_args_throws()
    {
        Action a = () => _ = new ChangeGroupManager(null!, new IdGenerator());
        Action b = () => _ = new ChangeGroupManager("dsp-1", null!);
        a.Should().Throw<ArgumentNullException>();
        b.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetSubscribedControls_returns_the_added_set()
    {
        var sut = new ChangeGroupManager("dsp-1", new IdGenerator());
        sut.BuildAddControl(ChangeGroupManager.PluginGroupId, "a");
        sut.BuildAddControl(ChangeGroupManager.PluginGroupId, "b");

        sut.GetSubscribedControls(ChangeGroupManager.PluginGroupId).Should().BeEquivalentTo(ExpectedAB);
        sut.GetSubscribedControls("unknown").Should().BeEmpty();
    }

    [Fact]
    public void HandleAutoPollPush_with_scalar_JValue_Result_does_not_throw()
    {
        // Issue #23: a Result that arrives as a JSON scalar (e.g. a
        // string or number) instead of the expected `{Changes:[...]}`
        // object used to throw InvalidOperationException ("Cannot
        // access child value on JValue") and crash the receive
        // thread, which silently broke unsolicited-feedback delivery
        // (#21). The manager must drop the malformed push silently.
        var sut = new ChangeGroupManager("dsp-1", new IdGenerator());
        int deltas = 0;
        sut.SetDeltaCallback(_ => deltas++);

        var scalar = new JsonRpcResponse { Id = 1, Result = JValue.CreateString("oops") };
        Action act = () => sut.HandleAutoPollPush(scalar);

        act.Should().NotThrow();
        deltas.Should().Be(0);
    }
}
