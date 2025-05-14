using System.Threading.Tasks;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.Helpers;
using AntiCaptchaApi.Net.Tests.IntegrationTests.Base;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.IntegrationTests.AnticaptchaRequests
{
    public class ProsopoProxylessRequestTests : AnticaptchaRequestTestBase<ProsopoSolution>
    {
        [Fact(Skip = "Find captcha website")]
        public async Task ShouldReturnCorrectCaptchaResult_WhenCallingAuthenticRequest()
        {
            await TestAuthenticRequest();
        }

        protected override ProsopoProxylessRequest CreateAuthenticRequest()
        {
            return new ProsopoProxylessRequest
            {
                WebsiteUrl = "https://example-service-that-uses-prosopo.com", // Placeholder URL
                WebsiteKey = "5FxMg5jAF3F8d8PrQezDMZh6ZbZd69kDt6FUVb1KaFpSgS2l" // Example key from AntiCaptcha docs
            };
        }

        protected override void AssertTaskResult(TaskResultResponse<ProsopoSolution> taskResult)
        {
            AssertHelper.NotNullNotEmpty(taskResult.Solution.Token);
            AssertHelper.NotNullNotEmpty(taskResult.Solution.UserAgent);
        }
    }
}