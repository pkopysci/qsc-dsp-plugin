// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using QscDspDevices.Plugin;
using QscDspDevices.Protocol.JsonRpc;
using Xunit;

namespace QscDspDevices.UnitTests.Plugin;

public sealed class LogRedactionTests
{
    [Fact]
    public void Render_replaces_password_field_in_params()
    {
        var request = new JsonRpcRequest
        {
            Id = 1,
            Method = "Logon",
            Params = new { User = "alice", Password = "hunter2" },
        };

        string rendered = LogRedaction.Render(request);

        rendered.Should().Contain("\"User\":\"alice\"");
        rendered.Should().Contain($"\"Password\":\"{LogRedaction.Placeholder}\"");
        rendered.Should().NotContain("hunter2");
    }

    [Fact]
    public void Render_is_case_insensitive_on_field_name()
    {
        var request = new JsonRpcRequest
        {
            Id = 1,
            Method = "Logon",
            Params = new { user = "bob", password = "secret-lower" },
        };

        string rendered = LogRedaction.Render(request);

        rendered.Should().NotContain("secret-lower");
        rendered.Should().Contain(LogRedaction.Placeholder);
    }

    [Fact]
    public void Render_passes_non_password_payloads_through_unchanged()
    {
        var request = new JsonRpcRequest
        {
            Id = 42,
            Method = "Control.Set",
            Params = new { Name = "tag", Value = 1 },
        };

        string rendered = LogRedaction.Render(request);

        rendered.Should().Contain("Control.Set");
        rendered.Should().Contain("\"Name\":\"tag\"");
        rendered.Should().NotContain(LogRedaction.Placeholder);
    }

    [Fact]
    public void Render_handles_null_params()
    {
        var request = new JsonRpcRequest { Id = 7, Method = "NoOp" };
        string rendered = LogRedaction.Render(request);
        rendered.Should().Contain("NoOp");
        rendered.Should().NotContain("params");
    }

    [Fact]
    public void Render_redacts_nested_password_fields()
    {
        var request = new JsonRpcRequest
        {
            Id = 1,
            Method = "Logon",
            Params = new { Auth = new { User = "alice", Password = "deep" } },
        };

        string rendered = LogRedaction.Render(request);

        rendered.Should().NotContain("deep");
        rendered.Should().Contain(LogRedaction.Placeholder);
    }

    [Fact]
    public void Render_does_not_mutate_original_request_params()
    {
        var request = new JsonRpcRequest
        {
            Id = 1,
            Method = "Logon",
            Params = new { User = "alice", Password = "hunter2" },
        };

        _ = LogRedaction.Render(request);

        // Re-render: still has the original password — proves the
        // helper redacted a clone rather than mutating in place.
        string rendered2 = LogRedaction.Render(request);
        rendered2.Should().Contain(LogRedaction.Placeholder);
        request.Params!.ToString().Should().Contain("hunter2");
    }
}
