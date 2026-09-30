using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using AntiCaptchaApi.Net.Tests.UnitTests.Client;
using AntiCaptchaApi.Net.Tests.UnitTests.Diagnostics;
using AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;
using AntiCaptchaApi.Net.Tests.UnitTests.Pipeline;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Security;

/// <summary>
/// US-5 for telemetry: no activity name, tag, status, event or baggage and no metric tag carries the client key, the
/// proxy (address, login, password), the website URL or key, or solution content.
/// </summary>
[Collection(TelemetryCollection.Name)]
public sealed class TelemetryRedactionTests : IDisposable
{
    private const string Key = "SECRET_KEY";
    private const string ProxyPassword = "PX_PASS";
    private const string ProxyLogin = "PX_LOGIN";
    private const string ProxyAddress = "px-secret-host.example";
    private const string WebsiteUrl = "https://secret-site.example/login";
    private const string WebsiteKey = "SECRET_SITE_KEY";
    private const string Token = "SECRET_TOKEN_VALUE";
    private const string UserAgent = "SECRET_USER_AGENT";
    private const string Echo = "key=" + Key + " proxy=" + ProxyLogin + ":" + ProxyPassword + "@" + ProxyAddress + " url=" + WebsiteUrl;

    private static readonly string[] Secrets = { Key, ProxyPassword, ProxyLogin, ProxyAddress, WebsiteUrl, WebsiteKey, Token, UserAgent };

    private static readonly IReadOnlyDictionary<string, Action<FakeHttpMessageHandler>> Scenarios = new Dictionary<string, Action<FakeHttpMessageHandler>>
    {
        ["success"] = h => h.Create(PipelineResponses.Created(1)).Poll(PipelineResponses.Processing).Poll(PipelineResponses.Ready(Token)),
        ["retry"] = h => h.Create(ClientTestContext.ApiError(25, "ERROR_PROXY_CONNECT_REFUSED", Echo))
            .Create(PipelineResponses.Created(2)).Poll(ClientTestContext.ApiError(12, "ERROR_CAPTCHA_UNSOLVABLE", Echo))
            .Create(PipelineResponses.Created(3)).Poll(PipelineResponses.Ready(Token)),
        ["transient"] = h => h.Create(PipelineResponses.Created(4))
            .Throw("getTaskResult", new HttpRequestException(Echo)).RespondJson("getTaskResult", "<html>" + Echo + "</html>")
            .Poll(PipelineResponses.Ready(Token)),
        ["fatal"] = h => h.Create(ClientTestContext.ApiError(1, "ERROR_KEY_DOES_NOT_EXIST", Echo)),
        ["fatal-unknown-code-echo"] = h => h.Create(PipelineResponses.Created(5)).Poll(ClientTestContext.ApiError(99, "ERROR_" + Key, Echo)),
    };

    // Whether the scenario ends in a successful solve, so that each path is known to have run as scripted.
    private static readonly IReadOnlyDictionary<string, bool> ExpectedSuccess = new Dictionary<string, bool>
    {
        ["success"] = true, ["retry"] = true, ["transient"] = true, ["fatal"] = false, ["fatal-unknown-code-echo"] = false,
    };

    private readonly ClientTestContext _context = new(Key);
    private readonly ActivityCollector _activities = new();
    private readonly MeasurementCollector _metrics = new();

    public static TheoryData<string> ScenarioNames => new(Scenarios.Keys);

    public void Dispose()
    {
        _activities.Dispose();
        _metrics.Dispose();
    }

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public async Task Telemetry_AllTagValuesAndEvents_ContainNoKeyOrProxyPassword(string scenario)
    {
        Scenarios[scenario](_context.Handler);
        var task = new TurnstileTask(WebsiteUrl, WebsiteKey)
        {
            Proxy = new Proxy(ProxyType.Http, ProxyAddress, 8080) { Login = ProxyLogin, Password = ProxyPassword },
            UserAgent = UserAgent,
        };

        var result = await _context.Client.TrySolveAsync(task);

        Assert.Equal(ExpectedSuccess[scenario], result.IsSuccess);

        var texts = _activities.Stopped.SelectMany(ActivityTexts).Concat(_metrics.All.SelectMany(m => m.Tags.Prepend(m.Instrument))).ToList();
        Assert.Contains("AntiCaptcha.Solve", texts); // Non-vacuous: the solve and its metrics were captured.
        Assert.Contains(texts, t => t.StartsWith("anticaptcha.solves", StringComparison.Ordinal));
        foreach (var text in texts)
        {
            foreach (var secret in Secrets)
            {
                Assert.False(text.Contains(secret, StringComparison.Ordinal), $"[{scenario}] telemetry contains \"{secret}\": {text}");
            }
        }
    }

    private static IEnumerable<string> ActivityTexts(Activity activity)
    {
        yield return activity.OperationName;
        yield return activity.DisplayName;
        yield return activity.StatusDescription ?? "";
        yield return activity.TraceStateString ?? "";
        foreach (var tag in activity.TagObjects)
        {
            yield return $"{tag.Key}={tag.Value}";
        }
        foreach (var item in activity.Baggage)
        {
            yield return $"{item.Key}={item.Value}";
        }
        foreach (var e in activity.Events)
        {
            yield return e.Name;
            foreach (var tag in e.Tags)
            {
                yield return $"{tag.Key}={tag.Value}";
            }
        }
        foreach (var link in activity.Links)
        {
            foreach (var tag in link.Tags ?? Enumerable.Empty<KeyValuePair<string, object?>>())
            {
                yield return $"{tag.Key}={tag.Value}";
            }
        }
    }
}
