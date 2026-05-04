// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

namespace QscDspDevices.Connectivity.Ecp;

/// <summary>
/// Credentials for an ECP <c>login</c> handshake. Mirrors the QRC
/// <c>LogonCredentials</c> shape: the connection adapter looks them
/// up at login time, so a hot credential change is picked up on the
/// next reconnect cycle without re-constructing the manager.
/// </summary>
/// <param name="Username">User account name (case-sensitive).</param>
/// <param name="Pin">PIN (numeric or alphanumeric).</param>
internal sealed record EcpCredentials(string Username, string Pin);
