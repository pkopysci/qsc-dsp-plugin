// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using System.Collections.Generic;

namespace QscDspDevices.Protocol;

/// <summary>
/// Maps numeric JSON-RPC and QSC-specific error codes received from a Q-SYS
/// Core to the typed <see cref="QrcErrorCode"/> enum.
/// </summary>
/// <remarks>
/// Codes the plugin does not recognise are classified as
/// <see cref="QrcErrorCode.ServerError"/> and a <c>Logger.Warn</c> is
/// expected to be raised by the caller so we learn about new codes
/// without crashing.
/// </remarks>
public static class QrcErrorClassifier
{
    private static readonly HashSet<int> Known = new()
    {
        (int)QrcErrorCode.ParseError,
        (int)QrcErrorCode.InvalidRequest,
        (int)QrcErrorCode.MethodNotFound,
        (int)QrcErrorCode.InvalidParams,
        (int)QrcErrorCode.ServerError,
        (int)QrcErrorCode.CoreOnStandby,
        (int)QrcErrorCode.ChangeGroupsExhausted,
        (int)QrcErrorCode.UnknownChangeGroup,
        (int)QrcErrorCode.UnknownComponentName,
        (int)QrcErrorCode.UnknownControl,
        (int)QrcErrorCode.IllegalMixerChannelIndex,
        (int)QrcErrorCode.LogonRequired,
    };

    /// <summary>
    /// Classifies the supplied numeric code.
    /// </summary>
    /// <param name="numericCode">The <c>code</c> field from a
    /// <see cref="JsonRpc.JsonRpcError"/>.</param>
    /// <param name="recognised">Set to <c>true</c> when the code is one we
    /// know; <c>false</c> when the caller should additionally log a Warn
    /// because the code is unfamiliar.</param>
    /// <returns>The typed code, or <see cref="QrcErrorCode.ServerError"/>
    /// when not recognised.</returns>
    public static QrcErrorCode Classify(int numericCode, out bool recognised)
    {
        if (Known.Contains(numericCode))
        {
            recognised = true;
            return (QrcErrorCode)numericCode;
        }

        recognised = false;
        return QrcErrorCode.ServerError;
    }
}
