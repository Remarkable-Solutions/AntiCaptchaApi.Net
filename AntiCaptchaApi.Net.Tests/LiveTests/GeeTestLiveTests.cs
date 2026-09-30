using System.Collections.Generic;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using AntiCaptchaApi.Net.Tests.LiveTests.GeeTest;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.LiveTests;

/// <summary>
/// GeeTest v3 and v4. Merges the 2.x classes <c>GeeRequestTestV3(Proxyless)RequestTests</c>,
/// <c>GeeRequestTestProxylessV3RequestTests</c> (a duplicate proxyless v3 test against a fixed seloger.com challenge,
/// which 2.x marked unsolvable) and <c>GeeRequestTestV4(Proxyless)RequestTests</c>.
/// </summary>
[Trait("Category", "Live")]
[Collection("Live")]
public class GeeTestLiveTests
{
    private const string V3WebsiteUrl = "https://www.geetest.com/en/demo";
    private const string V4Unsolvable = "Unsolvable in 2.x (the GeeTest v4 target site is gone); needs a working v4 demo page.";

    [LiveFact]
    public async Task SolveV3_Proxyless_ReturnsSolution()
    {
        var (gt, challenge) = await GeeTestTokens.FetchAsync();

        var solution = await LiveEnvironment.CreateClient().SolveAsync(new GeeTestV3Task(V3WebsiteUrl, gt, challenge));

        AssertV3(solution);
    }

    [LiveFact(RequiresProxy = true)]
    public async Task SolveV3_WithProxy_ReturnsSolution()
    {
        var (gt, challenge) = await GeeTestTokens.FetchAsync();
        var task = new GeeTestV3Task(V3WebsiteUrl, gt, challenge)
        {
            UserAgent = LiveEnvironment.UserAgent,
            Proxy = LiveEnvironment.Proxy,
        };

        var solution = await LiveEnvironment.CreateClient().SolveAsync(task);

        AssertV3(solution);
    }

    [LiveFact(Skip = V4Unsolvable)]
    public async Task SolveV4_Proxyless_ReturnsSolution()
    {
        var (gt, challenge) = await GeeTestTokens.FetchAsync();
        var task = new GeeTestV4Task("http://www.supremenewyork.com", gt) { Challenge = challenge };

        var solution = await LiveEnvironment.CreateClient().SolveAsync(task);

        AssertV4(solution);
    }

    [LiveFact(Skip = V4Unsolvable, RequiresProxy = true)]
    public async Task SolveV4_WithProxy_ReturnsSolution()
    {
        var (gt, challenge) = await GeeTestTokens.FetchAsync();
        var task = new GeeTestV4Task("http://www.supremenewyork.com", gt)
        {
            Challenge = challenge,
            InitParameters = new Dictionary<string, string> { ["riskType"] = "slide" },
            UserAgent = LiveEnvironment.UserAgent,
            Proxy = LiveEnvironment.Proxy,
        };

        var solution = await LiveEnvironment.CreateClient().SolveAsync(task);

        AssertV4(solution);
    }

    private static void AssertV3(GeeTestV3Solution solution)
    {
        Assert.False(string.IsNullOrEmpty(solution.Challenge));
        Assert.False(string.IsNullOrEmpty(solution.Validate));
        Assert.False(string.IsNullOrEmpty(solution.Seccode));
    }

    private static void AssertV4(GeeTestV4Solution solution)
    {
        Assert.False(string.IsNullOrEmpty(solution.CaptchaId));
        Assert.False(string.IsNullOrEmpty(solution.LotNumber));
        Assert.False(string.IsNullOrEmpty(solution.PassToken));
        Assert.False(string.IsNullOrEmpty(solution.GenTime));
        Assert.False(string.IsNullOrEmpty(solution.CaptchaOutput));
    }
}
