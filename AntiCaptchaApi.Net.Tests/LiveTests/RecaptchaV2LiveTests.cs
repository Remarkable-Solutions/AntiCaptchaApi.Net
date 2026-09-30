using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.LiveTests;

/// <summary>Ports 2.x <c>RecaptchaV2ProxylessRequestTests</c> (which sent an Enterprise request by mistake).</summary>
[Trait("Category", "Live")]
[Collection("Live")]
public class RecaptchaV2LiveTests
{
    [LiveFact]
    public async Task Solve_Proxyless_ReturnsResponse()
    {
        var task = new RecaptchaV2Task("http://http.myjino.ru/recaptcha/test-get.php", "6Lc_aCMTAAAAABx7u2W0WPXnVbI_v6ZdbM6rYf16");

        var solution = await LiveEnvironment.CreateClient().SolveAsync(task);

        Assert.False(string.IsNullOrEmpty(solution.GRecaptchaResponse));
    }
}
