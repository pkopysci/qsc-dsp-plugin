// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using Newtonsoft.Json.Linq;
using QscDspDevices.Protocol.JsonRpc;
using QscDspDevices.Protocol.Logging;
using Xunit;

namespace QscDspDevices.UnitTests.Protocol.Logging;

/// <summary>
/// Unit tests for <see cref="RedactingDebugFormatter"/>. Pin: only the
/// Logon method's Password field is redacted; everything else passes
/// through verbatim; the in-flight request object is not mutated.
/// </summary>
public sealed class RedactingDebugFormatterTests
{
    [Fact]
    public void Logon_request_redacts_Password_field()
    {
        var request = new JsonRpcRequest
        {
            Id = 1,
            Method = "Logon",
            Params = new { User = "alice", Password = "p4ssw0rd" },
        };

        string formatted = RedactingDebugFormatter.Format(request);
        var parsed = JObject.Parse(formatted);

        parsed["method"]!.ToString().Should().Be("Logon");
        var p = (JObject)parsed["params"]!;
        p["User"]!.ToString().Should().Be("alice");
        p["Password"]!.ToString().Should().Be(RedactingDebugFormatter.Redaction);
    }

    [Fact]
    public void Lowercase_password_field_is_also_redacted()
    {
        // The QSC docs use "Password" but reference clients have shipped both
        // casings. Defend in depth.
        var request = new JsonRpcRequest
        {
            Id = 7,
            Method = "Logon",
            Params = new Dictionary<string, object> { { "user", "alice" }, { "password", "p4ss" } },
        };

        string formatted = RedactingDebugFormatter.Format(request);
        var parsed = JObject.Parse(formatted);
        ((JObject)parsed["params"]!)["password"]!.ToString().Should().Be(RedactingDebugFormatter.Redaction);
    }

    [Fact]
    public void Non_Logon_request_is_unchanged()
    {
        var request = new JsonRpcRequest
        {
            Id = 42,
            Method = "Control.Set",
            Params = new { Name = "mic1.gain", Value = -40.0 },
        };

        string formatted = RedactingDebugFormatter.Format(request);
        var parsed = JObject.Parse(formatted);

        parsed["method"]!.ToString().Should().Be("Control.Set");
        var p = (JObject)parsed["params"]!;
        p["Name"]!.ToString().Should().Be("mic1.gain");
        p["Value"]!.ToObject<double>().Should().BeApproximately(-40.0, 1e-9);
    }

    [Fact]
    public void Logon_request_with_null_params_does_not_throw()
    {
        var request = new JsonRpcRequest { Id = 1, Method = "Logon", Params = null };
        Action act = () => RedactingDebugFormatter.Format(request);
        act.Should().NotThrow();
    }

    [Fact]
    public void Original_request_is_not_mutated_by_redaction()
    {
        var paramsObj = new Dictionary<string, object> { { "User", "alice" }, { "Password", "secret" } };
        var request = new JsonRpcRequest { Id = 1, Method = "Logon", Params = paramsObj };

        _ = RedactingDebugFormatter.Format(request);

        // The original dictionary is untouched.
        paramsObj["Password"].Should().Be("secret");
    }

    [Fact]
    public void Format_with_null_request_throws()
    {
        Action act = () => RedactingDebugFormatter.Format(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
