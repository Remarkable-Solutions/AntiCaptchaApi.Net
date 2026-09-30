using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Tests.Helpers;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests;

public class SolveCaptchaAsyncTests
{
    private const string UnsolvableJson =
        "{\"errorId\":12,\"errorCode\":\"ERROR_CAPTCHA_UNSOLVABLE\",\"errorDescription\":\"Captcha could not be solved\"}";

    [Fact]
    public async Task Solve_InvalidRequest_DoesNotRetryAndSendsNoHttp()
    {
        var handler = new FakeHttpMessageHandler();
        var client = CreateClient(handler, solveAsyncRetries: 3);

        var result = await client.SolveCaptchaAsync(new RecaptchaV2ProxylessRequest());

        Assert.Equal(0, handler.RequestCount);
        Assert.Equal("BadRequest", result.ErrorCode);
        Assert.False(string.IsNullOrEmpty(result.ErrorDescription));
        Assert.Equal("BadRequest", result.CreateTaskResponse.ErrorCode);
    }

    [Fact]
    public async Task Solve_InvalidRequestWithHugeRetryCount_ReturnsWithoutRetrying()
    {
        // Each retry of a locally invalid request re-runs validation; with int.MaxValue retries the old
        // behavior spins for minutes, so returning promptly proves the loop body ran once.
        var handler = new FakeHttpMessageHandler();
        var client = CreateClient(handler, solveAsyncRetries: int.MaxValue);

        var solveTask = Task.Run(() => client.SolveCaptchaAsync(new RecaptchaV2ProxylessRequest()));
        var completed = await Task.WhenAny(solveTask, Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.Same(solveTask, completed);
        Assert.Equal("BadRequest", (await solveTask).ErrorCode);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task Solve_TaskErrorStatus_RetriesUpToConfiguredCount()
    {
        var handler = new FakeHttpMessageHandler();
        for (var i = 0; i < 3; i++)
        {
            handler.EnqueueJson("{\"errorId\":0,\"taskId\":" + (100 + i) + "}");
            handler.EnqueueJson(UnsolvableJson);
        }
        var client = CreateClient(handler, solveAsyncRetries: 3);

        var result = await client.SolveCaptchaAsync(new RecaptchaV2ProxylessRequest
        {
            WebsiteUrl = "https://example.com/recaptcha",
            WebsiteKey = "6Le-wvkSAAAAAPBMRTvw0Q4Muexq9bi0DJwx_mJ-",
        });

        Assert.Equal(TaskStatusType.Error, result.Status);
        Assert.Equal(6, handler.RequestCount);
        Assert.Equal(3, handler.RequestUris.Count(u => u!.AbsolutePath == "/createTask"));
        Assert.Equal(102, result.CreateTaskResponse.TaskId);
    }

    private static AnticaptchaClient CreateClient(FakeHttpMessageHandler handler, int solveAsyncRetries) =>
        new("test-key",
            new ClientConfig { SolveAsyncRetries = solveAsyncRetries, DelayTimeBetweenCheckingTaskResultMs = 1 },
            new HttpClient(handler));
}
