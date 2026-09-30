using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.LiveTests;

[Trait("Category", "Live")]
[Collection("Live")]
public class ProsopoLiveTests
{
    // Skipped in 2.x as well: the URL is a placeholder and the key is the docs example.
    private const string NoTargetSite = "Find captcha website (skipped in 2.x too).";
    private const string WebsiteUrl = "https://example-service-that-uses-prosopo.com";
    private const string WebsiteKey = "5FxMg5jAF3F8d8PrQezDMZh6ZbZd69kDt6FUVb1KaFpSgS2l";

    [LiveFact(Skip = NoTargetSite)]
    public async Task Solve_Proxyless_ReturnsToken()
    {
        var solution = await LiveEnvironment.CreateClient().SolveAsync(new ProsopoTask(WebsiteUrl, WebsiteKey));

        Assert.False(string.IsNullOrEmpty(solution.Token));
        Assert.False(string.IsNullOrEmpty(solution.UserAgent));
    }

    [LiveFact(Skip = NoTargetSite, RequiresProxy = true)]
    public async Task Solve_WithProxy_ReturnsToken()
    {
        var task = new ProsopoTask(WebsiteUrl, WebsiteKey)
        {
            UserAgent = LiveEnvironment.UserAgent,
            Proxy = LiveEnvironment.Proxy,
        };

        var solution = await LiveEnvironment.CreateClient().SolveAsync(task);

        Assert.False(string.IsNullOrEmpty(solution.Token));
        Assert.False(string.IsNullOrEmpty(solution.UserAgent));
    }
}
