using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.LiveTests;

[Trait("Category", "Live")]
[Collection("Live")]
public class TurnstileLiveTests
{
    private const string WebsiteUrl = "https://react-turnstile.vercel.app/";
    private const string WebsiteKey = "3x00000000000000000000FF";

    [LiveFact]
    public async Task Solve_Proxyless_ReturnsToken()
    {
        var solution = await LiveEnvironment.CreateClient().SolveAsync(new TurnstileTask(WebsiteUrl, WebsiteKey));

        Assert.False(string.IsNullOrEmpty(solution.Token));
    }

    [LiveFact(RequiresProxy = true)]
    public async Task Solve_WithProxy_ReturnsToken()
    {
        var task = new TurnstileTask(WebsiteUrl, WebsiteKey)
        {
            UserAgent = LiveEnvironment.UserAgent,
            Proxy = LiveEnvironment.Proxy,
        };

        var solution = await LiveEnvironment.CreateClient().SolveAsync(task);

        Assert.False(string.IsNullOrEmpty(solution.Token));
    }
}
