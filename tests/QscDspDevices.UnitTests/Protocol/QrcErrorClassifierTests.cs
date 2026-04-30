// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.Protocol;
using Xunit;

namespace QscDspDevices.UnitTests.Protocol;

/// <summary>
/// Unit tests for <see cref="QrcErrorClassifier"/>.
/// </summary>
public sealed class QrcErrorClassifierTests
{
    [Theory]
    [InlineData(-32700, QrcErrorCode.ParseError)]
    [InlineData(-32600, QrcErrorCode.InvalidRequest)]
    [InlineData(-32601, QrcErrorCode.MethodNotFound)]
    [InlineData(-32602, QrcErrorCode.InvalidParams)]
    [InlineData(-32603, QrcErrorCode.ServerError)]
    [InlineData(-32604, QrcErrorCode.CoreOnStandby)]
    [InlineData(5, QrcErrorCode.ChangeGroupsExhausted)]
    [InlineData(6, QrcErrorCode.UnknownChangeGroup)]
    [InlineData(7, QrcErrorCode.UnknownComponentName)]
    [InlineData(8, QrcErrorCode.UnknownControl)]
    [InlineData(9, QrcErrorCode.IllegalMixerChannelIndex)]
    [InlineData(10, QrcErrorCode.LogonRequired)]
    public void Classify_known_codes_returns_typed_value_and_recognised_true(int numeric, QrcErrorCode expected)
    {
        QrcErrorCode actual = QrcErrorClassifier.Classify(numeric, out bool recognised);

        actual.Should().Be(expected);
        recognised.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(99)]
    [InlineData(-1)]
    [InlineData(-99999)]
    public void Classify_unknown_code_returns_ServerError_with_recognised_false(int numeric)
    {
        QrcErrorCode actual = QrcErrorClassifier.Classify(numeric, out bool recognised);

        actual.Should().Be(QrcErrorCode.ServerError);
        recognised.Should().BeFalse();
    }
}
