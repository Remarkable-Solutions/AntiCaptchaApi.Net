using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.LiveTests;

/// <summary>Ports 2.x <c>RecaptchaV3RequestTests</c> and <c>RecaptchaV3EnterpriseRequestTests</c>.</summary>
[Trait("Category", "Live")]
[Collection("Live")]
public class RecaptchaV3LiveTests
{
    [LiveTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Solve_ReturnsResponse(bool isEnterprise)
    {
        var task = new RecaptchaV3Task("https://www.netflix.com/login", "6Lf8hrcUAAAAAIpQAFW2VFjtiYnThOjZOA5xvLyR")
        {
            IsEnterprise = isEnterprise,
        };

        var solution = await LiveEnvironment.CreateClient().SolveAsync(task);

        Assert.False(string.IsNullOrEmpty(solution.GRecaptchaResponse));
    }
}
