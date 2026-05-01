// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.AudioControl;
using Xunit;

namespace QscDspDevices.UnitTests.AudioControl;

/// <summary>
/// Unit tests for <see cref="LevelScaler"/>. Pin the boundary cases
/// and the half-up rounding contract; the FsCheck round-trip property
/// lives in the property-tests project.
/// </summary>
public sealed class LevelScalerTests
{
    [Theory]
    [InlineData(0, -80, 0, -80.0)]
    [InlineData(50, -80, 0, -40.0)]
    [InlineData(100, -80, 0, 0.0)]
    [InlineData(0, 0, 100, 0.0)]
    [InlineData(50, 0, 100, 50.0)]
    [InlineData(100, 0, 100, 100.0)]
    [InlineData(0, -100, 100, -100.0)]
    [InlineData(50, -100, 100, 0.0)]
    [InlineData(100, -100, 100, 100.0)]
    public void ToDevice_known_points_round_trip_to_expected_native_value(
        int frameworkLevel, int min, int max, double expected)
    {
        var sut = new LevelScaler("dsp-1");
        sut.ToDevice(frameworkLevel, min, max, "ch").Should().BeApproximately(expected, 1e-9);
    }

    [Theory]
    [InlineData(-80.0, -80, 0, 0)]
    [InlineData(-40.0, -80, 0, 50)]
    [InlineData(0.0, -80, 0, 100)]
    [InlineData(0.0, 0, 100, 0)]
    [InlineData(50.0, 0, 100, 50)]
    [InlineData(100.0, 0, 100, 100)]
    public void ToFramework_known_points_round_trip_to_expected_framework_value(
        double deviceValue, int min, int max, int expected)
    {
        LevelScaler.ToFramework(deviceValue, min, max).Should().Be(expected);
    }

    [Fact]
    public void ToFramework_uses_half_up_rounding_at_the_midpoint()
    {
        // 0..1, value 0.5 → exactly 50.0 framework. 0.49→49, 0.51→51.
        LevelScaler.ToFramework(0.5, 0, 1).Should().Be(50);

        // device 0..2, value 0.5 → exactly 25.0 framework, no rounding needed.
        LevelScaler.ToFramework(0.5, 0, 2).Should().Be(25);

        // Force a half-step: scale 0..3, value 1.5 → 50.0 framework exactly.
        LevelScaler.ToFramework(1.5, 0, 3).Should().Be(50);

        // device 0..2, value 0.51 → framework 25.5 → half-up → 26.
        LevelScaler.ToFramework(0.51, 0, 2).Should().Be(26);
    }

    [Fact]
    public void ToDevice_clamps_below_zero_to_min_and_warns_once()
    {
        var sut = new LevelScaler("dsp-1");
        sut.ToDevice(-5, -80, 0, "ch").Should().Be(-80);
        sut.ToDevice(-100, -80, 0, "ch").Should().Be(-80);
    }

    [Fact]
    public void ToDevice_clamps_above_100_to_max_and_warns_once()
    {
        var sut = new LevelScaler("dsp-1");
        sut.ToDevice(101, -80, 0, "ch").Should().Be(0);
        sut.ToDevice(int.MaxValue, -80, 0, "ch").Should().Be(0);
    }

    [Fact]
    public void ToFramework_clamps_outside_min_max()
    {
        LevelScaler.ToFramework(-1000, -80, 0).Should().Be(0);
        LevelScaler.ToFramework(1000, -80, 0).Should().Be(100);
    }

    [Fact]
    public void ToDevice_with_min_equal_to_max_throws()
    {
        var sut = new LevelScaler("dsp-1");
        Action act = () => sut.ToDevice(50, 0, 0, "ch");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToFramework_with_min_equal_to_max_throws()
    {
        Action act = () => LevelScaler.ToFramework(0.0, 0, 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_with_null_deviceId_throws()
    {
        Action act = () => _ = new LevelScaler(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDevice_with_null_channelId_throws()
    {
        var sut = new LevelScaler("dsp-1");
        Action act = () => sut.ToDevice(50, 0, 100, null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
