using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Client;

public class PushAntiGateVariableTests
{
    private const int TaskId = 123456;
    private const string Name = "my_late_variable";
    private const string Success = "{\"errorId\":0,\"status\":\"success\"}";

    private readonly ClientTestContext _context = new();

    public static TheoryData<object, string> SupportedValues => new()
    {
        { "The value which worker should be waiting for", "\"The value which worker should be waiting for\"" },
        { "", "\"\"" },
        { true, "true" },
        { false, "false" },
        { 42, "42" },
        { -9_000_000_000L, "-9000000000" },
        { 1.5d, "1.5" },
        { 0.1m, "0.1" },
        { 7.25f, "7.25" },
        { (byte)7, "7" },
        { (sbyte)-7, "-7" },
        { (short)-300, "-300" },
        { (ushort)300, "300" },
        { 4_000_000_000u, "4000000000" },
        { ulong.MaxValue, "18446744073709551615" },
        { new JsonObject { ["login"] = "user", ["remember"] = true }, "{\"login\":\"user\",\"remember\":true}" },
        { new JsonArray(1, "two"), "[1,\"two\"]" },
        { JsonValue.Create("node")!, "\"node\"" },
    };

    public static TheoryData<object> UnsupportedValues => new()
    {
        new DateTime(2026, 9, 30),
        new object(),
        new List<string> { "a" },
        'c',
        Guid.Empty,
        double.NaN,
        double.PositiveInfinity,
        float.NegativeInfinity,
    };

    [Theory]
    [MemberData(nameof(SupportedValues))]
    public async Task Push_SupportedValueTypes_SerializeCorrectly(object value, string expectedValueJson)
    {
        _context.Handler.RespondJson("pushAntiGateVariable", Success);

        await _context.Client.PushAntiGateVariableAsync(TaskId, Name, value);

        var body = Assert.Single(_context.Handler.RequestsFor("pushAntiGateVariable")).Json;
        var expected = new JsonObject
        {
            ["clientKey"] = ClientTestContext.Key,
            ["taskId"] = TaskId,
            ["name"] = Name,
            ["value"] = JsonNode.Parse(expectedValueJson),
        };
        Assert.True(JsonNode.DeepEquals(expected, body), body?.ToJsonString());
        Assert.Equal(expectedValueJson, body!["value"]!.ToJsonString());
    }

    [Theory]
    [MemberData(nameof(UnsupportedValues))]
    public async Task Push_UnsupportedType_ThrowsArgumentException_NoRequest(object value)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _context.Client.PushAntiGateVariableAsync(TaskId, Name, value));

        Assert.Equal("value", exception.ParamName);
        Assert.Empty(_context.Handler.Requests);
    }

    [Fact]
    public async Task Push_NullValue_ThrowsArgumentNull_NoRequest()
    {
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _context.Client.PushAntiGateVariableAsync(TaskId, Name, null!));

        Assert.Equal("value", exception.ParamName);
        Assert.Empty(_context.Handler.Requests);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Push_EmptyName_ThrowsArgumentException_NoRequest(string? name)
    {
        var exception = await Assert.ThrowsAnyAsync<ArgumentException>(() => _context.Client.PushAntiGateVariableAsync(TaskId, name!, "v"));

        Assert.Equal("name", exception.ParamName);
        Assert.Empty(_context.Handler.Requests);
    }

    [Fact]
    public async Task Push_UnsupportedType_MessageNamesTypeButNotValue()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _context.Client.PushAntiGateVariableAsync(TaskId, Name, new Uri("https://user:SECRET_VALUE@example.com/")));

        Assert.Contains("System.Uri", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET_VALUE", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Push_ApiError_Throws()
    {
        _context.Handler.RespondJson("pushAntiGateVariable", ClientTestContext.ApiError(16, "ERROR_NO_SUCH_CAPCHA_ID", "Task you are requesting does not exist"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.PushAntiGateVariableAsync(TaskId, Name, "v"));

        Assert.Equal(AntiCaptchaErrorCode.NoSuchCaptchaId, exception.Error.Code);
        Assert.Equal(TaskId, exception.TaskId);
    }
}
