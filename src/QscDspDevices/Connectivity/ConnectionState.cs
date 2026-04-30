// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

namespace QscDspDevices.Connectivity;

/// <summary>
/// The four states the connection manager can be in. Documented in
/// <c>ARCHITECTURE.md</c> "Connection state — M2".
/// </summary>
public enum ConnectionState
{
    /// <summary>The plugin is not connected to any Q-SYS Core.</summary>
    Disconnected,

    /// <summary>The transport is mid-Connect; <see cref="Connected"/> or a fault is imminent.</summary>
    Connecting,

    /// <summary>The transport is open and the plugin is exchanging frames.</summary>
    Connected,

    /// <summary>External Disconnect() has been called; cleanup is in flight.</summary>
    Disconnecting,
}
