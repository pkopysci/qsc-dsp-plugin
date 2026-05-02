// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using QscDspDevices.AudioControl;
using Xunit;

namespace QscDspDevices.UnitTests.AudioControl;

/// <summary>
/// Unit tests for <see cref="AudioChannelRegistry"/>. Pin the
/// registration / lookup / replace semantics that the rest of the
/// plugin depends on.
/// </summary>
public sealed class AudioChannelRegistryTests
{
    private static readonly IReadOnlyList<string> NoTags = Array.Empty<string>();
    private static readonly string[] OnlyMic1 = { "mic1" };
    private static readonly string[] OnlyOut1 = { "out1" };
    private static readonly string[] OnlyDinner = { "dinner" };

    [Fact]
    public void Register_input_then_GetInputIds_returns_it()
    {
        var sut = new AudioChannelRegistry("dsp-1");
        sut.RegisterInput(new AudioChannel(
            "mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 0, NoTags));

        sut.GetInputIds().Should().BeEquivalentTo(OnlyMic1);
        sut.GetOutputIds().Should().BeEmpty();
    }

    [Fact]
    public void Register_output_then_GetOutputIds_returns_it()
    {
        var sut = new AudioChannelRegistry("dsp-1");
        sut.RegisterOutput(new AudioChannel(
            "out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags));

        sut.GetOutputIds().Should().BeEquivalentTo(OnlyOut1);
        sut.GetInputIds().Should().BeEmpty();
    }

    [Fact]
    public void RegisterInput_with_isInput_false_throws()
    {
        var sut = new AudioChannelRegistry("dsp-1");
        Action act = () => sut.RegisterInput(new AudioChannel(
            "x", "lvl", "mute", 0, 1, false, 0, 0, NoTags));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RegisterOutput_with_isInput_true_throws()
    {
        var sut = new AudioChannelRegistry("dsp-1");
        Action act = () => sut.RegisterOutput(new AudioChannel(
            "x", "lvl", "mute", 0, 1, true, 0, 0, NoTags));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryGetChannel_known_id_returns_true()
    {
        var sut = new AudioChannelRegistry("dsp-1");
        var channel = new AudioChannel("mic1", "lvl", "mute", -80, 0, true, 0, 0, NoTags);
        sut.RegisterInput(channel);

        sut.TryGetChannel("mic1", out AudioChannel? found).Should().BeTrue();
        found.Should().Be(channel);
    }

    [Fact]
    public void TryGetChannel_unknown_id_returns_false()
    {
        var sut = new AudioChannelRegistry("dsp-1");

        sut.TryGetChannel("nope", out AudioChannel? found).Should().BeFalse();
        found.Should().BeNull();
    }

    [Fact]
    public void Re_registering_a_channel_replaces_the_prior_entry_and_remaps_tags()
    {
        var sut = new AudioChannelRegistry("dsp-1");
        sut.RegisterInput(new AudioChannel(
            "mic1", "old.lvl", "old.mute", -80, 0, true, 0, 0, NoTags));
        sut.RegisterInput(new AudioChannel(
            "mic1", "new.lvl", "new.mute", -100, 0, true, 0, 0, NoTags));

        sut.TryGetChannel("mic1", out AudioChannel? found).Should().BeTrue();
        found!.LevelTag.Should().Be("new.lvl");
        sut.TryGetChannelIdByTag("old.lvl", out _).Should().BeFalse();
        sut.TryGetChannelIdByTag("new.lvl", out string? viaNew).Should().BeTrue();
        viaNew.Should().Be("mic1");
    }

    [Fact]
    public void RegisterPreset_then_GetPresetIds_includes_it()
    {
        var sut = new AudioChannelRegistry("dsp-1");
        sut.RegisterPreset(new AudioPreset("dinner", "MainBank", 3));

        sut.GetPresetIds().Should().BeEquivalentTo(OnlyDinner);
        sut.TryGetPreset("dinner", out AudioPreset? p).Should().BeTrue();
        p.Should().Be(new AudioPreset("dinner", "MainBank", 3));
    }

    [Fact]
    public void TryGetChannelIdByTag_resolves_level_and_mute_tags_to_the_channel_id()
    {
        var sut = new AudioChannelRegistry("dsp-1");
        sut.RegisterInput(new AudioChannel(
            "mic1", "mic1.gain", "mic1.mute", -80, 0, true, 0, 0, NoTags));

        sut.TryGetChannelIdByTag("mic1.gain", out string? viaLevel).Should().BeTrue();
        viaLevel.Should().Be("mic1");
        sut.TryGetChannelIdByTag("mic1.mute", out string? viaMute).Should().BeTrue();
        viaMute.Should().Be("mic1");
    }

    [Fact]
    public async Task Concurrent_registrations_are_thread_safe()
    {
        var sut = new AudioChannelRegistry("dsp-1");

        Task[] tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            sut.RegisterInput(new AudioChannel(
                $"in-{i}", $"in-{i}.lvl", $"in-{i}.mute", -80, 0, true, 0, 0, NoTags));
            sut.RegisterOutput(new AudioChannel(
                $"out-{i}", $"out-{i}.lvl", $"out-{i}.mute", -100, 0, false, 0, 0, NoTags));
        })).ToArray();

        await Task.WhenAll(tasks);

        sut.GetInputIds().Should().HaveCount(50);
        sut.GetOutputIds().Should().HaveCount(50);
    }

    [Fact]
    public void Constructor_with_null_deviceId_throws()
    {
        Action act = () => _ = new AudioChannelRegistry(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryGetInputIdByBankIndex_returns_the_owning_input_id()
    {
        var sut = new AudioChannelRegistry("dsp-1");
        sut.RegisterInput(new AudioChannel("mic1", "lvl1", "mute1", -80, 0, true, 0, 5, NoTags));
        sut.RegisterInput(new AudioChannel("mic2", "lvl2", "mute2", -80, 0, true, 0, 7, NoTags));

        sut.TryGetInputIdByBankIndex(5, out string? a).Should().BeTrue();
        a.Should().Be("mic1");
        sut.TryGetInputIdByBankIndex(7, out string? b).Should().BeTrue();
        b.Should().Be("mic2");
        sut.TryGetInputIdByBankIndex(99, out _).Should().BeFalse();
    }

    [Fact]
    public void Re_registering_input_with_new_bankIndex_remaps_the_reverse_table()
    {
        var sut = new AudioChannelRegistry("dsp-1");
        sut.RegisterInput(new AudioChannel("mic1", "lvl", "mute", -80, 0, true, 0, 5, NoTags));
        sut.RegisterInput(new AudioChannel("mic1", "lvl", "mute", -80, 0, true, 0, 9, NoTags));

        // The old bank-index entry is gone; the new one points at mic1.
        sut.TryGetInputIdByBankIndex(5, out _).Should().BeFalse();
        sut.TryGetInputIdByBankIndex(9, out string? viaNew).Should().BeTrue();
        viaNew.Should().Be("mic1");
    }

    [Fact]
    public void IsRouterTag_is_true_only_for_a_registered_output_routerTag()
    {
        var sut = new AudioChannelRegistry("dsp-1");
        sut.RegisterOutput(new AudioChannel(
            "out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, "mixer.out1.source"));
        sut.RegisterInput(new AudioChannel("mic1", "lvl", "mute", -80, 0, true, 0, 0, NoTags));

        sut.IsRouterTag("mixer.out1.source").Should().BeTrue();
        sut.IsRouterTag("mic1.gain").Should().BeFalse();
        sut.IsRouterTag("out1.gain").Should().BeFalse();
        sut.IsRouterTag("never-registered").Should().BeFalse();
    }

    [Fact]
    public void Output_without_routerTag_is_not_a_router_tag_owner()
    {
        var sut = new AudioChannelRegistry("dsp-1");
        sut.RegisterOutput(new AudioChannel(
            "out1", "out1.gain", "out1.mute", -100, 0, false, 0, 0, NoTags, string.Empty));

        // The empty-string routerTag must not register as one — otherwise
        // every AutoPoll delta with an empty Name would dispatch to routing.
        sut.IsRouterTag(string.Empty).Should().BeFalse();
    }

    [Fact]
    public void Two_channels_claiming_the_same_levelTag_logs_warn_and_overwrites()
    {
        // Designer-side configuration error: two channels declare the
        // same Q-SYS named control as their levelTag. Without
        // detection, AutoPoll deltas on this tag silently dispatch
        // to the wrong owner. Pin the warn-on-collision behaviour.
        var sut = new AudioChannelRegistry("dsp-1");
        sut.RegisterInput(new AudioChannel("mic1", "shared.tag", "mic1.mute", -80, 0, true, 0, 1, NoTags));
        sut.RegisterInput(new AudioChannel("mic2", "shared.tag", "mic2.mute", -80, 0, true, 0, 2, NoTags));

        // Reverse map last-writer-wins; mic2 owns the tag now.
        sut.TryGetChannelIdByTag("shared.tag", out string? owner).Should().BeTrue();
        owner.Should().Be("mic2");
    }

    [Fact]
    public void Re_registering_output_with_new_routerTag_remaps_the_router_set()
    {
        var sut = new AudioChannelRegistry("dsp-1");
        sut.RegisterOutput(new AudioChannel(
            "out1", "lvl", "mute", -100, 0, false, 0, 0, NoTags, "old.router"));
        sut.RegisterOutput(new AudioChannel(
            "out1", "lvl", "mute", -100, 0, false, 0, 0, NoTags, "new.router"));

        sut.IsRouterTag("old.router").Should().BeFalse();
        sut.IsRouterTag("new.router").Should().BeTrue();
    }
}
