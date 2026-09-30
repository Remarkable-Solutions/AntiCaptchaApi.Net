using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.Helpers;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests;

public class TaskResultConverterTests
{
    private const long CreateTime = 1700000000;
    private const long EndTime = 1700000012;

    private const string ProsopoReadyJson =
        "{\"errorId\":0,\"status\":\"ready\",\"solution\":{\"token\":\"prosopo-token\",\"userAgent\":\"ua\"}," +
        "\"cost\":\"0.00100\",\"ip\":\"1.2.3.4\",\"createTime\":1700000000,\"endTime\":1700000012,\"solveCount\":0}";

    private const string AmazonWafReadyJson =
        "{\"errorId\":0,\"status\":\"ready\",\"solution\":{\"token\":\"aws-waf-token\"}," +
        "\"cost\":\"0.00100\",\"ip\":\"1.2.3.4\",\"createTime\":1700000000,\"endTime\":1700000012,\"solveCount\":0}";

    private const string TurnstileReadyJson =
        "{\"errorId\":0,\"status\":\"ready\",\"solution\":{\"token\":\"turnstile-token\"}," +
        "\"cost\":\"0.00100\",\"ip\":\"1.2.3.4\",\"createTime\":1700000000,\"endTime\":1700000012,\"solveCount\":0}";

    private const string UnsolvableJson =
        "{\"errorId\":12,\"errorCode\":\"ERROR_CAPTCHA_UNSOLVABLE\",\"errorDescription\":\"Captcha could not be solved\"}";

    [Fact]
    public async Task GetTaskResultAsync_ProsopoReady_SetsCreateAndEndTime()
    {
        var result = await GetTaskResultAsync<ProsopoSolution>(ProsopoReadyJson);

        AssertReadyWithTimes(result);
        Assert.Equal("prosopo-token", result.Solution.Token);
    }

    [Fact]
    public async Task GetTaskResultAsync_ProsopoApiError_StatusIsError()
    {
        var result = await GetTaskResultAsync<ProsopoSolution>(UnsolvableJson);

        Assert.Equal(TaskStatusType.Error, result.Status);
    }

    [Fact]
    public async Task GetTaskResultAsync_AmazonWafReady_SetsCreateAndEndTime()
    {
        var result = await GetTaskResultAsync<AmazonWafSolution>(AmazonWafReadyJson);

        AssertReadyWithTimes(result);
        Assert.Equal("aws-waf-token", result.Solution.Token);
    }

    [Fact]
    public async Task GetTaskResultAsync_AmazonWafApiError_StatusIsError()
    {
        var result = await GetTaskResultAsync<AmazonWafSolution>(UnsolvableJson);

        Assert.Equal(TaskStatusType.Error, result.Status);
    }

    [Fact]
    public async Task GetTaskResultAsync_TurnstileReady_SetsCreateAndEndTime()
    {
        var result = await GetTaskResultAsync<TurnstileSolution>(TurnstileReadyJson);

        AssertReadyWithTimes(result);
    }

    [Fact]
    public async Task GetTaskResultAsync_TurnstileApiError_StatusIsError()
    {
        var result = await GetTaskResultAsync<TurnstileSolution>(UnsolvableJson);

        Assert.Equal(TaskStatusType.Error, result.Status);
    }

    private static async Task<TaskResultResponse<TSolution>> GetTaskResultAsync<TSolution>(string json)
        where TSolution : BaseSolution, new()
    {
        var handler = new FakeHttpMessageHandler().EnqueueJson(json);
        var client = new AnticaptchaClient("test-key", new ClientConfig(), new HttpClient(handler));
        return await client.GetTaskResultAsync<TSolution>(42, CancellationToken.None);
    }

    private static void AssertReadyWithTimes<TSolution>(TaskResultResponse<TSolution> result)
        where TSolution : BaseSolution
    {
        Assert.Equal(TaskStatusType.Ready, result.Status);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(CreateTime).UtcDateTime, result.CreateTimeUtc);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(EndTime).UtcDateTime, result.EndTimeUtc);
    }
}
