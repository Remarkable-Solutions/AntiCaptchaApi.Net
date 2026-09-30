using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using AntiCaptchaApi.Net.Tests.UnitTests.Client;
using AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;
using AntiCaptchaApi.Net.Tests.UnitTests.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Security;

/// <summary>FR-9 / US-5: the client key and proxy passwords never leak through errors or <c>ToString()</c>.</summary>
public class RedactionTests
{
    private const string Key = "SECRET_KEY_123";
    private const string ProxyPassword = "PX_PASS_456";
    private const string Echo = "key=" + Key + " proxy=proxyUser:" + ProxyPassword + "@203.0.113.10:1080";

    private static readonly Proxy SecretProxy = new(ProxyType.Http, "203.0.113.10", 1080) { Login = "proxyUser", Password = ProxyPassword };

    // Each scenario scripts one createTask failure; several deliberately echo both secrets back.
    private static readonly IReadOnlyDictionary<string, Action<FakeHttpMessageHandler>> Scenarios = new Dictionary<string, Action<FakeHttpMessageHandler>>
    {
        ["api-error-echo"] = h => h.RespondJson("createTask", ClientTestContext.ApiError(25, "ERROR_PROXY_CONNECT_REFUSED", Echo)),
        ["api-error-unknown-code-echo"] = h => h.RespondJson("createTask", ClientTestContext.ApiError(99, "ERROR_" + Key, Echo)),
        ["network-failure-echo"] = h => h.Throw("createTask", new HttpRequestException(Echo, new InvalidOperationException(Echo))),
        ["status-503-reason-echo"] = h => h.Script("createTask", _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { ReasonPhrase = Echo, Content = new StringContent(Echo) })),
        ["status-400-body-echo"] = h => h.Script("createTask", _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(Echo) })),
        ["request-timeout"] = h => h.Script("createTask", async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("unreachable");
        }),
        ["httpclient-timeout-echo"] = h => h.Throw("createTask", new TaskCanceledException(Echo, new TimeoutException(Echo))),
        ["invalid-json-echo"] = h => h.RespondJson("createTask", "<html>" + Echo + "</html>"),
        ["wrong-shape-json-echo"] = h => h.RespondJson("createTask", "{\"errorId\":0,\"taskId\":\"" + Echo + "\"}"),
    };

    public static TheoryData<string> ScenarioNames => new(Scenarios.Keys);

    private static void AssertNoSecrets(string? text, string context)
    {
        if (text is null)
        {
            return;
        }
        Assert.False(text.Contains(Key, StringComparison.Ordinal), $"{context} contains the client key: {text}");
        Assert.False(text.Contains(ProxyPassword, StringComparison.Ordinal), $"{context} contains the proxy password: {text}");
    }

    private static void AssertExceptionHasNoSecrets(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            AssertNoSecrets(current.Message, current.GetType().Name + ".Message");
        }
        AssertNoSecrets(exception.ToString(), "exception.ToString()");
        if (exception is AntiCaptchaException { Error: var error })
        {
            AssertNoSecrets(error.ToString(), "AntiCaptchaError.ToString()");
            AssertNoSecrets(error.Description, "AntiCaptchaError.Description");
            AssertNoSecrets(error.RawCode, "AntiCaptchaError.RawCode");
        }
    }

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public async Task AllTransportErrorPaths_ExceptionMessageAndToString_ContainNoSecrets(string scenario)
    {
        var context = new ClientTestContext(Key, new AntiCaptchaClientOptions { RequestTimeout = TimeSpan.FromMilliseconds(50) });
        Scenarios[scenario](context.Handler);
        var task = new TurnstileTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { Proxy = SecretProxy, UserAgent = TaskSamples.UserAgent };

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => context.Client.CreateTaskAsync(task));

        AssertExceptionHasNoSecrets(exception);
    }

    [Fact]
    public async Task GetTaskResult_ApiErrorEchoingKey_ContainsNoKey()
    {
        var context = new ClientTestContext(Key);
        context.Handler.RespondJson("getTaskResult", ClientTestContext.ApiError(16, "ERROR_NO_SUCH_CAPCHA_ID", Echo));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => context.Client.GetTaskResultAsync<TurnstileSolution>(1));

        AssertNoSecrets(exception.ToString().Replace(ProxyPassword, string.Empty), "getTaskResult exception");
    }

    [Fact]
    public async Task AntiCaptchaError_ToString_ContainsNoSecrets()
    {
        var context = new ClientTestContext(Key);
        context.Handler.RespondJson("createTask", ClientTestContext.ApiError(12, "ERROR_CAPTCHA_UNSOLVABLE", Echo));
        var task = new RecaptchaV2Task(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { Proxy = SecretProxy, UserAgent = TaskSamples.UserAgent };

        var error = (await Assert.ThrowsAsync<AntiCaptchaException>(() => context.Client.CreateTaskAsync(task))).Error;

        AssertNoSecrets(error.ToString(), "AntiCaptchaError.ToString()");
        Assert.Contains("CaptchaUnsolvable", error.ToString());
    }

    [Fact]
    public void Client_ToString_ContainsNoKey()
    {
        AssertNoSecrets(new AntiCaptchaClient(Key).ToString(), "AntiCaptchaClient.ToString()");
        AssertNoSecrets(new AntiCaptchaClient(Key, new HttpClient()).ToString(), "AntiCaptchaClient.ToString()");
    }

    public static TheoryData<string, string> Samples()
    {
        var data = new TheoryData<string, string>();
        foreach (var (type, samples) in TaskSamples.All)
        {
            foreach (var (caseName, _) in samples)
            {
                data.Add(type.Name, caseName);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void Task_ToString_WithProxy_ContainsNoPassword(string typeName, string caseName)
    {
        var task = TaskSamples.Get(typeName, caseName) with { Proxy = SecretProxy };

        var text = task.ToString();

        AssertNoSecrets(text, $"{typeName}.{caseName}.ToString()");
        Assert.Contains("Password = ***", text);
    }

    public static TheoryData<string> InvalidTasksWithProxy => new()
    {
        "port-out-of-range",
        "empty-address",
        "undefined-type",
        "proxy-not-supported",
        "user-agent-missing",
    };

    private static CaptchaTask InvalidTask(string name) => name switch
    {
        "port-out-of-range" => new TurnstileTask("not-a-url", "") { Proxy = SecretProxy with { Port = 0 }, UserAgent = TaskSamples.UserAgent },
        "empty-address" => new FunCaptchaTask(TaskSamples.WebsiteUrl, "PUBLIC_KEY") { Proxy = SecretProxy with { Address = "" }, UserAgent = TaskSamples.UserAgent },
        "undefined-type" => new ProsopoTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { Proxy = SecretProxy with { Type = (ProxyType)42 }, UserAgent = TaskSamples.UserAgent },
        "proxy-not-supported" => new ImageToTextTask(TaskSamples.ImageBody) { Proxy = SecretProxy },
        "user-agent-missing" => new RecaptchaV2Task(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { Proxy = SecretProxy },
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    [Theory]
    [MemberData(nameof(InvalidTasksWithProxy))]
    public async Task ValidationException_Message_ContainsNoProxyPassword(string name)
    {
        var context = new ClientTestContext(Key);

        var exception = await Assert.ThrowsAsync<AntiCaptchaValidationException>(() => context.Client.CreateTaskAsync(InvalidTask(name)));

        Assert.NotEmpty(exception.Errors);
        AssertNoSecrets(exception.Message, "validation message");
        AssertNoSecrets(exception.ToString(), "validation ToString()");
        AssertNoSecrets(string.Join(";", exception.Errors.Select(e => e.ToString())), "ValidationError.ToString()");
        Assert.Empty(context.Handler.Requests);
    }

    public static TheoryData<string> SolvePollFailures => new() { "api-error-echo", "network-failure-echo", "invalid-json-echo" };

    private static void ScriptPollFailure(FakeHttpMessageHandler handler, string name, int times)
    {
        for (var i = 0; i < times; i++)
        {
            _ = name switch
            {
                "api-error-echo" => handler.RespondJson("getTaskResult", ClientTestContext.ApiError(16, "ERROR_NO_SUCH_CAPCHA_ID", Echo)),
                "network-failure-echo" => handler.Throw("getTaskResult", new HttpRequestException(Echo, new InvalidOperationException(Echo))),
                "invalid-json-echo" => handler.RespondJson("getTaskResult", "<html>" + Echo + "</html>"),
                _ => throw new ArgumentOutOfRangeException(nameof(name)),
            };
        }
    }

    [Theory]
    [MemberData(nameof(SolvePollFailures))]
    public async Task Solve_PollErrorEchoingSecrets_ExceptionAndResultContainNoSecrets(string scenario)
    {
        var context = new ClientTestContext(Key);
        var task = new TurnstileTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { Proxy = SecretProxy, UserAgent = TaskSamples.UserAgent };
        var options = new SolveOptions { MaxConsecutivePollErrors = 0 };
        context.Handler.RespondJson("createTask", "{\"errorId\":0,\"taskId\":1}").RespondJson("createTask", "{\"errorId\":0,\"taskId\":2}");
        ScriptPollFailure(context.Handler, scenario, 2);

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => context.Client.SolveAsync(task, options));
        var result = await context.Client.TrySolveAsync(task, options);

        AssertExceptionHasNoSecrets(exception);
        Assert.NotNull(result.Error);
        AssertNoSecrets(result.ToString(), "SolveResult.ToString()");
        AssertNoSecrets(result.Error!.Description, "SolveResult.Error.Description");
    }

    private const string KeyEcho = "invalid clientKey " + Key;

    // Account and task-management methods: the API method each one calls, and how to call it.
    private static readonly IReadOnlyDictionary<string, (string Endpoint, Func<AntiCaptchaClient, Task> Call)> AccountMethods =
        new Dictionary<string, (string, Func<AntiCaptchaClient, Task>)>
        {
            ["GetBalance"] = ("getBalance", c => c.GetBalanceAsync()),
            ["GetQueueStats"] = ("getQueueStats", c => c.GetQueueStatsAsync(QueueType.ImageToTextEng)),
            ["Report"] = ("reportIncorrectImageCaptcha", c => c.ReportAsync(7, Report.IncorrectImageCaptcha)),
            ["PushAntiGateVariable"] = ("pushAntiGateVariable", c => c.PushAntiGateVariableAsync(7, "code", "value")),
            ["GetSpendingStats"] = ("getSpendingStats", c => c.GetSpendingStatsAsync()),
            ["GetAppStats"] = ("getAppStats", c => c.GetAppStatsAsync(123)),
        };

    public static TheoryData<string, string> AccountMethodFailures()
    {
        var data = new TheoryData<string, string>();
        foreach (var method in AccountMethods.Keys)
        {
            foreach (var failure in new[] { "api-error-echo", "network-failure-echo", "invalid-json-echo" })
            {
                data.Add(method, failure);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(AccountMethodFailures))]
    public async Task AccountMethods_ErrorPaths_ContainNoSecrets(string method, string failure)
    {
        var context = new ClientTestContext(Key);
        var (endpoint, call) = AccountMethods[method];
        _ = failure switch
        {
            "api-error-echo" => context.Handler.RespondJson(endpoint, ClientTestContext.ApiError(1, "ERROR_KEY_DOES_NOT_EXIST", KeyEcho)),
            "network-failure-echo" => context.Handler.Throw(endpoint, new HttpRequestException(KeyEcho, new InvalidOperationException(KeyEcho))),
            "invalid-json-echo" => context.Handler.RespondJson(endpoint, "<html>" + KeyEcho + "</html>"),
            _ => throw new ArgumentOutOfRangeException(nameof(failure)),
        };

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => call(context.Client));

        Assert.Single(context.Handler.RequestsFor(endpoint));
        AssertExceptionHasNoSecrets(exception);
    }
}
