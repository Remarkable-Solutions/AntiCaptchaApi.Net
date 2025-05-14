using System.Threading.Tasks;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.Helpers;
using AntiCaptchaApi.Net.Tests.IntegrationTests.Base;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.IntegrationTests.AnticaptchaRequests
{
    public class FriendlyCaptchaProxylessRequestTests : AnticaptchaRequestTestBase<ProsopoSolution> // Using ProsopoSolution as it's identical
    {
        [Fact]
        public async Task ShouldReturnCorrectCaptchaResult_WhenCallingAuthenticRequest()
        {
            await TestAuthenticRequest();
        }

        protected override FriendlyCaptchaProxylessRequest CreateAuthenticRequest()
        {
            // Note: Using example key from documentation. 
            // A live Friendly Captcha test page URL would be ideal for a more comprehensive "authentic" test.
            return new FriendlyCaptchaProxylessRequest
            {
                WebsiteUrl = "https://example-service-that-uses-friendlycaptcha.com", // Placeholder URL
                WebsiteKey = "FCMDESUD3M34857N" // Example key from AntiCaptcha docs for Friendly Captcha
            };
        }

        protected override void AssertTaskResult(TaskResultResponse<ProsopoSolution> taskResult)
        {
            AssertHelper.NotNullNotEmpty(taskResult.Solution.Token);
            AssertHelper.NotNullNotEmpty(taskResult.Solution.UserAgent); // UserAgent is part of the solution for FriendlyCaptcha
        }
    }
}