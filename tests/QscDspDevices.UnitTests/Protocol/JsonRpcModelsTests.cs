// Copyright (c) 2026 QscDspDevices Contributors. Licensed under MIT.

using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QscDspDevices.Protocol.JsonRpc;
using Xunit;

namespace QscDspDevices.UnitTests.Protocol;

/// <summary>
/// Round-trip tests for the JSON-RPC POCOs. Verifies the
/// JsonProperty attribute names, default JsonRpc version literal, and
/// the IsNotification / IsError computed properties on
/// <see cref="JsonRpcResponse"/>.
/// </summary>
public sealed class JsonRpcModelsTests
{
    [Fact]
    public void JsonRpcRequest_serialises_in_documented_field_order()
    {
        var request = new JsonRpcRequest
        {
            Id = 7,
            Method = "Component.Set",
            Params = new { Name = "x" },
        };

        string json = JsonConvert.SerializeObject(request);

        json.Should().StartWith("{\"jsonrpc\":\"2.0\"");
        json.Should().Contain("\"id\":7");
        json.Should().Contain("\"method\":\"Component.Set\"");
        json.Should().Contain("\"params\":{\"Name\":\"x\"}");
    }

    [Fact]
    public void JsonRpcRequest_omits_null_params()
    {
        var request = new JsonRpcRequest { Id = 1, Method = "NoOp", Params = null };
        string json = JsonConvert.SerializeObject(request);
        json.Should().NotContain("\"params\"");
    }

    [Fact]
    public void JsonRpcResponse_with_id_and_result_is_neither_notification_nor_error()
    {
        const string source = """{"jsonrpc":"2.0","id":42,"result":{"ok":true}}""";
        JsonRpcResponse? r = JsonConvert.DeserializeObject<JsonRpcResponse>(source);

        r.Should().NotBeNull();
        r!.Id.Should().Be(42);
        r.IsNotification.Should().BeFalse();
        r.IsError.Should().BeFalse();
        r.Result.Should().NotBeNull();
    }

    [Fact]
    public void JsonRpcResponse_with_method_and_no_id_is_a_notification()
    {
        const string source = """{"jsonrpc":"2.0","method":"EngineStatus","params":{"State":"Active"}}""";
        JsonRpcResponse? r = JsonConvert.DeserializeObject<JsonRpcResponse>(source);

        r.Should().NotBeNull();
        r!.Id.Should().BeNull();
        r.IsNotification.Should().BeTrue();
        r.Method.Should().Be("EngineStatus");
    }

    [Fact]
    public void JsonRpcResponse_with_error_is_classified_as_error()
    {
        const string source = """{"jsonrpc":"2.0","id":3,"error":{"code":-32604,"message":"Standby"}}""";
        JsonRpcResponse? r = JsonConvert.DeserializeObject<JsonRpcResponse>(source);

        r.Should().NotBeNull();
        r!.IsError.Should().BeTrue();
        r.Error.Should().NotBeNull();
        r.Error!.Code.Should().Be(-32604);
        r.Error.Message.Should().Be("Standby");
    }

    [Fact]
    public void JsonRpcError_data_field_round_trips_an_arbitrary_token()
    {
        const string source = """{"code":7,"message":"unknown","data":{"detail":"foo"}}""";
        JsonRpcError? e = JsonConvert.DeserializeObject<JsonRpcError>(source);

        e.Should().NotBeNull();
        e!.Data.Should().BeOfType<JObject>();
        e.Data!["detail"]!.Value<string>().Should().Be("foo");
    }
}
