using System.Text.Json;
using System.Text.Json.Nodes;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Internal.Json.Envelopes;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Json;

public class AntiCaptchaJsonContextTests
{
    [Fact]
    public void Serialize_CreateTaskRequest_IsCompactAndOmitsNulls()
    {
        var request = new CreateTaskRequest("TEST_KEY", new JsonObject { ["type"] = "TurnstileTaskProxyless" });

        var json = JsonSerializer.Serialize(request, AntiCaptchaJsonContext.Default.CreateTaskRequest);

        Assert.Equal("{\"clientKey\":\"TEST_KEY\",\"task\":{\"type\":\"TurnstileTaskProxyless\"},\"softId\":1023}", json);
    }

    [Fact]
    public void Deserialize_ErrorEnvelope_ReadsAllFields()
    {
        const string json = "{\"errorId\":1,\"errorCode\":\"ERROR_KEY_DOES_NOT_EXIST\",\"errorDescription\":\"Account authorization key not found in the system\"}";

        var envelope = JsonSerializer.Deserialize(json, AntiCaptchaJsonContext.Default.ApiErrorEnvelope);

        Assert.NotNull(envelope);
        Assert.Equal(1, envelope.ErrorId);
        Assert.Equal("ERROR_KEY_DOES_NOT_EXIST", envelope.ErrorCode);
        Assert.Equal("Account authorization key not found in the system", envelope.ErrorDescription);
    }

    [Fact]
    public void Serialize_CreateTaskRequest_AlwaysContainsSoftId1023()
    {
        var request = new CreateTaskRequest("TEST_KEY", new JsonObject { ["type"] = "ImageToTextTask" })
        {
            LanguagePool = "en",
            CallbackUrl = "https://example.com/callback",
        };

        var node = JsonNode.Parse(JsonSerializer.Serialize(request, AntiCaptchaJsonContext.Default.CreateTaskRequest))!;

        Assert.Equal(1023, (int)node["softId"]!);
        Assert.Equal("en", (string?)node["languagePool"]);
        Assert.Equal("https://example.com/callback", (string?)node["callbackUrl"]);
        Assert.False(typeof(CreateTaskRequest).GetProperty(nameof(CreateTaskRequest.SoftId))!.CanWrite, "softId must not be settable by callers");
    }

    [Fact]
    public void Deserialize_CreateTaskResponse_ReadsTaskId()
    {
        var response = JsonSerializer.Deserialize("{\"errorId\":0,\"taskId\":7654321}", AntiCaptchaJsonContext.Default.CreateTaskResponse);

        Assert.Equal(7654321, response!.TaskId);
    }
}
