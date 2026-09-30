using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Internal.Converters;
using AntiCaptchaApi.Net.Internal.Helpers;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.Helpers;
using Newtonsoft.Json;
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

    [Fact]
    public void ReadJson_ParallelAntiGateResponses_EachResultUsesOwnJson()
    {
        const int count = 200;
        var converters = HttpHelper.Converters.ToArray();
        var results = new TaskResultResponse<AntiGateSolution>?[count];

        Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, i =>
        {
            results[i] = JsonConvert.DeserializeObject<TaskResultResponse<AntiGateSolution>>(AntiGateReadyJson(i), converters);
        });

        for (var i = 0; i < count; i++)
        {
            var solution = results[i]?.Solution;
            Assert.NotNull(solution);
            Assert.Equal(i.ToString(), (string?)solution!.Cookies?["id"]);
            Assert.Equal(i.ToString(), (string?)solution.Fingerprint?["id"]);
        }
    }

    [Fact]
    public void Converters_HaveNoMutableInstanceFields()
    {
        var converterTypes = new[]
        {
            typeof(TaskResultConverter<TurnstileSolution>),
            typeof(AntiGateTaskResultConverter),
        };

        var offenders = new List<string>();
        foreach (var converterType in converterTypes)
        {
            for (var type = converterType; type != null && type != typeof(JsonConverter); type = type.BaseType)
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                offenders.AddRange(fields
                    .Where(f => !f.IsInitOnly && f.GetCustomAttribute<ObsoleteAttribute>() == null)
                    .Select(f => $"{type.Name}.{f.Name}"));
            }
        }

        Assert.True(offenders.Count == 0, "Mutable instance fields on shared converters: " + string.Join(", ", offenders.Distinct()));
    }

    private static string AntiGateReadyJson(int id) =>
        "{\"errorId\":0,\"status\":\"ready\",\"solution\":{\"url\":\"https://example.com/" + id + "\",\"domain\":\"example.com\"," +
        "\"cookies\":{\"id\":\"" + id + "\"},\"localStorage\":{\"id\":\"" + id + "\"},\"fingerprint\":{\"id\":\"" + id + "\"}}," +
        "\"cost\":\"0.00100\",\"ip\":\"1.2.3.4\",\"createTime\":1700000000,\"endTime\":1700000012,\"solveCount\":0}";

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
