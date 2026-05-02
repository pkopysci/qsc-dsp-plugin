// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using gcu_common_utils.GenericEventArgs;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol;
using QscDspDevices.Protocol.JsonRpc;

namespace QscDspDevices.Connectivity.Redundancy;

/// <summary>
/// Subscribes to a <see cref="JsonRpcDispatcher.NotificationReceived"/>
/// event and forwards every <c>EngineStatus</c> push as an
/// <see cref="EngineState"/> to a registered callback. Malformed
/// pushes (missing <c>State</c>, unknown values) log
/// <c>Logger.Warn</c> and are otherwise silent.
/// </summary>
/// <remarks>
/// One observer per <see cref="ConnectionManager"/>. The pair
/// coordinator owns one per slot; the callback identifies the slot
/// implicitly via the registration site.
/// </remarks>
public sealed class EngineStatusObserver : IDisposable
{
    private readonly string _deviceId;
    private readonly JsonRpcDispatcher _dispatcher;
    private readonly Action<EngineState> _onStateChanged;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineStatusObserver"/> class
    /// and subscribes to the supplied dispatcher.
    /// </summary>
    /// <param name="deviceId">The owning device id (for log messages).</param>
    /// <param name="dispatcher">The dispatcher whose notifications are observed.</param>
    /// <param name="onStateChanged">Callback invoked with each parsed state.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public EngineStatusObserver(string deviceId, JsonRpcDispatcher dispatcher, Action<EngineState> onStateChanged)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(onStateChanged);

        _deviceId = deviceId;
        _dispatcher = dispatcher;
        _onStateChanged = onStateChanged;
        _dispatcher.NotificationReceived += OnNotification;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _dispatcher.NotificationReceived -= OnNotification;
    }

    private void OnNotification(object? sender, GenericSingleEventArgs<JsonRpcResponse> args)
    {
        JsonRpcResponse response = args.Arg;
        if (!string.Equals(response.Method, "EngineStatus", StringComparison.Ordinal))
        {
            return;
        }

        // EngineStatus is delivered as a notification — the State is in
        // the `params` block (the dispatcher routes server pushes via
        // Params, not Result).
        string? rawState = response.Params?["State"]?.ToString();
        if (string.IsNullOrEmpty(rawState))
        {
            Log.Warn(_deviceId, "EngineStatus push missing State field; ignoring.");
            return;
        }

        EngineState state = rawState switch
        {
            "Active" => EngineState.Active,
            "Standby" => EngineState.Standby,
            "Idle" => EngineState.Idle,
            _ => EngineState.Unknown,
        };

        if (state == EngineState.Unknown)
        {
            Log.Warn(_deviceId, $"EngineStatus push reported unknown State '{rawState}'; ignoring.");
            return;
        }

        _onStateChanged(state);
    }
}
