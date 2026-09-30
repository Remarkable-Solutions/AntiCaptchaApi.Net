using System.Collections.Generic;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.LiveTests;

/// <summary>Ports 2.x <c>RecaptchaV2EnterpriseProxylessRequestTests</c> (its two facts sent the same task; merged).</summary>
[Trait("Category", "Live")]
[Collection("Live")]
public class RecaptchaV2EnterpriseLiveTests
{
    [LiveFact]
    public async Task Solve_Proxyless_ReturnsResponse()
    {
        var task = new RecaptchaV2EnterpriseTask("https://store.steampowered.com/join", "6LdIFr0ZAAAAAO3vz0O0OQrtAefzdJcWQM2TMYQH")
        {
            EnterprisePayload = new Dictionary<string, string>
            {
                ["test"] = "qwerty",
                ["secret"] = "AB_12345",
            },
        };

        var solution = await LiveEnvironment.CreateClient().SolveAsync(task);

        Assert.False(string.IsNullOrEmpty(solution.GRecaptchaResponse));
    }
}
