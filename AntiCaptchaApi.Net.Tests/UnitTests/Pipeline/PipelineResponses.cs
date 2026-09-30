using System.Globalization;
using System.Net.Http;
using AntiCaptchaApi.Net.Tests.UnitTests.Client;
using AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Pipeline;

/// <summary>Scripted <c>createTask</c>/<c>getTaskResult</c> payloads shared by the pipeline tests.</summary>
internal static class PipelineResponses
{
    public const string Processing = "{\"errorId\":0,\"status\":\"processing\"}";

    public const string ReadyWithoutSolution = "{\"errorId\":0,\"status\":\"ready\"}";

    public const long CreateTime = 1_700_000_000;

    public static string Created(int taskId) => $"{{\"errorId\":0,\"taskId\":{taskId}}}";

    /// <summary>A ready Turnstile result, solved 12 seconds after creation by worker <c>198.51.100.7</c>.</summary>
    public static string Ready(string token = "TOKEN", decimal cost = 0.002m) =>
        $"{{\"errorId\":0,\"status\":\"ready\",\"solution\":{{\"token\":\"{token}\"}},\"cost\":\"{cost.ToString(CultureInfo.InvariantCulture)}\","
        + $"\"ip\":\"198.51.100.7\",\"createTime\":{CreateTime},\"endTime\":{CreateTime + 12},\"solveCount\":\"1\"}}";

    public static string Error(int errorId, string code) => ClientTestContext.ApiError(errorId, code, "scripted " + code);

    public static FakeHttpMessageHandler Poll(this FakeHttpMessageHandler handler, string json) => handler.RespondJson("getTaskResult", json);

    public static FakeHttpMessageHandler PollNetworkFailure(this FakeHttpMessageHandler handler) =>
        handler.Throw("getTaskResult", new HttpRequestException("connection reset"));

    public static FakeHttpMessageHandler Create(this FakeHttpMessageHandler handler, string json) => handler.RespondJson("createTask", json);
}
