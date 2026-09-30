using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.LiveTests;

[Trait("Category", "Live")]
[Collection("Live")]
public class FunCaptchaLiveTests
{
    [LiveFact]
    public async Task Solve_Proxyless_ReturnsToken()
    {
        var task = new FunCaptchaTask(
            "https://demo.arkoselabs.com/?key=DF9C4D87-CB7B-4062-9FEB-BADB6ADA61E6",
            "DF9C4D87-CB7B-4062-9FEB-BADB6ADA61E6")
        {
            ApiJsSubdomain = "test",
            Data = "test",
        };

        var solution = await LiveEnvironment.CreateClient().SolveAsync(task);

        Assert.False(string.IsNullOrEmpty(solution.Token));
    }

    [LiveFact(RequiresProxy = true)]
    public async Task Solve_WithProxy_ReturnsToken()
    {
        var task = new FunCaptchaTask(
            "https://api.funcaptcha.com/fc/api/nojs/?pkey=69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC",
            "69A21A01-CC7B-B9C6-0F9A-E7FA06677FFC")
        {
            ApiJsSubdomain = "test",
            Data = "test",
            UserAgent = LiveEnvironment.UserAgent,
            Proxy = LiveEnvironment.Proxy,
        };

        var solution = await LiveEnvironment.CreateClient().SolveAsync(task);

        Assert.False(string.IsNullOrEmpty(solution.Token));
    }
}
