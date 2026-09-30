using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.LiveTests;

[Trait("Category", "Live")]
[Collection("Live")]
public class FriendlyCaptchaLiveTests
{
    // Example key from the anti-captcha docs; a real Friendly Captcha page would make this more meaningful (as in 2.x).
    private const string WebsiteUrl = "https://example-service-that-uses-friendlycaptcha.com";
    private const string WebsiteKey = "FCMDESUD3M34857N";

    [LiveFact]
    public async Task Solve_Proxyless_ReturnsToken()
    {
        var solution = await LiveEnvironment.CreateClient().SolveAsync(new FriendlyCaptchaTask(WebsiteUrl, WebsiteKey));

        Assert.False(string.IsNullOrEmpty(solution.Token));
        Assert.False(string.IsNullOrEmpty(solution.UserAgent));
    }

    [LiveFact(RequiresProxy = true)]
    public async Task Solve_WithProxy_ReturnsToken()
    {
        var task = new FriendlyCaptchaTask(WebsiteUrl, WebsiteKey)
        {
            Proxy = LiveEnvironment.Proxy,
            UserAgent = LiveEnvironment.UserAgent,
        };

        var solution = await LiveEnvironment.CreateClient().SolveAsync(task);

        Assert.False(string.IsNullOrEmpty(solution.Token));
        Assert.False(string.IsNullOrEmpty(solution.UserAgent));
    }
}
