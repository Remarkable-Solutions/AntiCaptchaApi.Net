using System.Threading.Tasks;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.Helpers;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.IntegrationTests.AnticaptchaRequests
{
    public class GeeRequestTestV3RequestTests : GeeRequestTestsBase<GeeTestV3Solution>
    {
        [Fact]
        public async Task ShouldReturnCorrectCaptchaResult_WhenCallingAuthenticRequest()
        {
            await TestAuthenticRequest();
        }

        protected override GeeTestV3Request CreateAuthenticRequest()
        {
            var (gt, websiteChallenge) = GetTokens();
            return new GeeTestV3Request()
            {
                WebsiteUrl = "https://www.geetest.com/en/demo",
                Gt = gt,
                Challenge = websiteChallenge,
                UserAgent = TestEnvironment.UserAgent,
                ProxyConfig = TestEnvironment.GetCurrentTestProxyConfig()
            };
        }

        protected override void AssertTaskResult(TaskResultResponse<GeeTestV3Solution> taskResult)
        {
            AssertHelper.NotNullNotEmpty(taskResult.Solution.Challenge);
            AssertHelper.NotNullNotEmpty(taskResult.Solution.Validate);
            AssertHelper.NotNullNotEmpty(taskResult.Solution.Seccode);
        }
    }
}