using System.Threading.Tasks;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.IntegrationTests.Base;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.IntegrationTests.AnticaptchaRequests
{
    public class FunCaptchaProxylessRequestTests : AnticaptchaRequestTestBase<FunCaptchaSolution>
    {
        private const string FunCaptchaUriExample = "https://demo.arkoselabs.com/?key=DF9C4D87-CB7B-4062-9FEB-BADB6ADA61E6";

        protected override FunCaptchaProxylessRequest CreateAuthenticRequest() =>
            new()
            {
                WebsiteUrl = FunCaptchaUriExample,
                WebsitePublicKey = "DF9C4D87-CB7B-4062-9FEB-BADB6ADA61E6",
                FunCaptchaApiJsSubdomain = "test",
                Data = "test",
            };

        [Fact]
        public async Task ShouldReturnCorrectCaptchaResult_WhenCallingAuthenticRequest()
        {
            await TestAuthenticRequest();
        }

        protected override void AssertTaskResult(TaskResultResponse<FunCaptchaSolution> taskResult)
        {
            Assert.NotNull(taskResult.Solution);
            Assert.NotNull(taskResult.Solution.Token);
        }
    }
}