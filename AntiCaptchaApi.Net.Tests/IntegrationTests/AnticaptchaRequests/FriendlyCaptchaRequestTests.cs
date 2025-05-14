using System.Threading.Tasks;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.Helpers;
using AntiCaptchaApi.Net.Tests.IntegrationTests.Base;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.IntegrationTests.AnticaptchaRequests;

public class FriendlyCaptchaRequestTests : AnticaptchaRequestTestBase<ProsopoSolution> // Using ProsopoSolution as it's identical
{
    [Fact]
    public async Task ShouldReturnCorrectCaptchaResult_WhenCallingAuthenticRequest()
    {
        if (!TestEnvironment.IsProxyDefined)
        {
            return; // Skip if proxy is not configured
        }
        await TestAuthenticRequest();
    }

    protected override FriendlyCaptchaRequest CreateAuthenticRequest()
    {
        // Note: Using example key from documentation. 
        // A live Friendly Captcha test page URL would be ideal for a more comprehensive "authentic" test.
        return new FriendlyCaptchaRequest
        {
            WebsiteUrl = "https://example-service-that-uses-friendlycaptcha.com", // Placeholder URL
            WebsiteKey = "FCMDESUD3M34857N", // Example key from AntiCaptcha docs for Friendly Captcha
            UserAgent = TestEnvironment.UserAgent,
            ProxyConfig = new ProxyConfig
            {
                ProxyType = Enums.ProxyTypeOption.Http,
                ProxyAddress = TestEnvironment.ProxyAddress,
                ProxyPort = int.Parse(TestEnvironment.ProxyPort),
                ProxyLogin = TestEnvironment.ProxyLogin,
                ProxyPassword = TestEnvironment.ProxyPassword
            }
        };
    }

    protected override void AssertTaskResult(TaskResultResponse<ProsopoSolution> taskResult)
    {
        AssertHelper.NotNullNotEmpty(taskResult.Solution.Token);
        AssertHelper.NotNullNotEmpty(taskResult.Solution.UserAgent);
    }
}