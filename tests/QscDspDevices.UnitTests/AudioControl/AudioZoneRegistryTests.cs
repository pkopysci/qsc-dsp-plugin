// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.AudioControl;
using Xunit;

namespace QscDspDevices.UnitTests.AudioControl;

/// <summary>
/// Unit tests for <see cref="AudioZoneRegistry"/>. Pin: pair-keyed
/// add/remove; duplicate-pair Add drops per framework spec; reverse
/// tag lookup for AutoPoll dispatch.
/// </summary>
public sealed class AudioZoneRegistryTests
{
    [Fact]
    public void TryRegister_a_new_pair_returns_true()
    {
        var sut = new AudioZoneRegistry("dsp-1");
        sut.TryRegister("mic1", "zoneA", "tag.A").Should().BeTrue();
    }

    [Fact]
    public void TryRegister_duplicate_pair_returns_false_and_keeps_the_prior_tag()
    {
        var sut = new AudioZoneRegistry("dsp-1");
        sut.TryRegister("mic1", "zoneA", "tag.A").Should().BeTrue();
        sut.TryRegister("mic1", "zoneA", "tag.A.different").Should().BeFalse();

        sut.TryGet("mic1", "zoneA", out string? tag).Should().BeTrue();
        tag.Should().Be("tag.A");
    }

    [Fact]
    public void TryGet_unknown_pair_returns_false()
    {
        var sut = new AudioZoneRegistry("dsp-1");
        sut.TryGet("nope", "nope", out string? tag).Should().BeFalse();
        tag.Should().BeNull();
    }

    [Fact]
    public void Remove_existing_pair_drops_only_that_row()
    {
        var sut = new AudioZoneRegistry("dsp-1");
        sut.TryRegister("mic1", "zoneA", "tag.A");
        sut.TryRegister("mic1", "zoneB", "tag.B");

        sut.Remove("mic1", "zoneA").Should().BeTrue();

        sut.TryGet("mic1", "zoneA", out _).Should().BeFalse();
        sut.TryGet("mic1", "zoneB", out string? tagB).Should().BeTrue();
        tagB.Should().Be("tag.B");
    }

    [Fact]
    public void Remove_unknown_pair_is_a_silent_no_op()
    {
        var sut = new AudioZoneRegistry("dsp-1");
        sut.Remove("nope", "nope").Should().BeFalse();
    }

    [Fact]
    public void TryGetPair_resolves_a_registered_tag_back_to_the_pair()
    {
        var sut = new AudioZoneRegistry("dsp-1");
        sut.TryRegister("mic1", "zoneA", "tag.A");

        sut.TryGetPair("tag.A", out (string ChannelId, string ZoneId) pair).Should().BeTrue();
        pair.Should().Be(("mic1", "zoneA"));
    }

    [Fact]
    public void IsZoneTag_is_true_for_registered_tags_only()
    {
        var sut = new AudioZoneRegistry("dsp-1");
        sut.TryRegister("mic1", "zoneA", "tag.A");

        sut.IsZoneTag("tag.A").Should().BeTrue();
        sut.IsZoneTag("tag.never-registered").Should().BeFalse();
    }

    [Fact]
    public void GetAll_returns_every_registered_triple()
    {
        var sut = new AudioZoneRegistry("dsp-1");
        sut.TryRegister("mic1", "zoneA", "tag.A");
        sut.TryRegister("mic1", "zoneB", "tag.B");
        sut.TryRegister("mic2", "zoneA", "tag.C");

        sut.GetAll().Should().HaveCount(3);
    }

    [Fact]
    public void Removed_tag_is_no_longer_a_zone_tag()
    {
        var sut = new AudioZoneRegistry("dsp-1");
        sut.TryRegister("mic1", "zoneA", "tag.A");
        sut.Remove("mic1", "zoneA");

        sut.IsZoneTag("tag.A").Should().BeFalse();
        sut.TryGetPair("tag.A", out _).Should().BeFalse();
    }

    [Fact]
    public void Constructor_with_null_deviceId_throws()
    {
        Action act = () => _ = new AudioZoneRegistry(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
