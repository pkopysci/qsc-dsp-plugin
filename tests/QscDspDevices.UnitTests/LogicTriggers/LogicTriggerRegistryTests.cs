// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.LogicTriggers;
using Xunit;

namespace QscDspDevices.UnitTests.LogicTriggers;

/// <summary>
/// Unit tests for <see cref="LogicTriggerRegistry"/>.
/// </summary>
public sealed class LogicTriggerRegistryTests
{
    [Fact]
    public void Register_then_TryGet_returns_the_tagName()
    {
        var sut = new LogicTriggerRegistry("dsp-1");
        sut.Register("rec", "rec.start");

        sut.TryGet("rec", out string? tag).Should().BeTrue();
        tag.Should().Be("rec.start");
    }

    [Fact]
    public void TryGet_unknown_id_returns_false()
    {
        var sut = new LogicTriggerRegistry("dsp-1");
        sut.TryGet("nope", out string? tag).Should().BeFalse();
        tag.Should().BeNull();
    }

    [Fact]
    public void Re_register_replaces_the_prior_tag_and_remaps_the_reverse_table()
    {
        var sut = new LogicTriggerRegistry("dsp-1");
        sut.Register("rec", "rec.start");
        sut.Register("rec", "rec.start.v2");

        sut.TryGet("rec", out string? tag).Should().BeTrue();
        tag.Should().Be("rec.start.v2");
        sut.IsTriggerTag("rec.start").Should().BeFalse();
        sut.IsTriggerTag("rec.start.v2").Should().BeTrue();
    }

    [Fact]
    public void IsTriggerTag_true_only_for_registered_tags()
    {
        var sut = new LogicTriggerRegistry("dsp-1");
        sut.Register("rec", "rec.start");

        sut.IsTriggerTag("rec.start").Should().BeTrue();
        sut.IsTriggerTag("never-registered").Should().BeFalse();
    }

    [Fact]
    public void TryGetIdByTag_resolves_a_registered_tag_back_to_the_id()
    {
        var sut = new LogicTriggerRegistry("dsp-1");
        sut.Register("rec", "rec.start");

        sut.TryGetIdByTag("rec.start", out string? id).Should().BeTrue();
        id.Should().Be("rec");
    }

    [Fact]
    public void GetAll_returns_every_registered_pair()
    {
        var sut = new LogicTriggerRegistry("dsp-1");
        sut.Register("rec", "rec.start");
        sut.Register("stop", "stop.cmd");

        sut.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public void Constructor_with_null_deviceId_throws()
    {
        Action act = () => _ = new LogicTriggerRegistry(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
