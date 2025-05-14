using System.Threading.Tasks;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.Helpers;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.IntegrationTests.AnticaptchaRequests
{
    public class GeeRequestTestV4ProxylessRequestTests : GeeRequestTestsBase<GeeTestV4Solution>
    {
        [Fact]
        public async Task ShouldReturnCorrectCaptchaResult_WhenCallingAuthenticRequest()
        {
            Assert.False(true, "Unsolvable request"); //TODO!
            await TestAuthenticRequest();
            
        }

        protected override GeeTestV4ProxylessRequest CreateAuthenticRequest()
        {
            var (websiteKey, websiteChallenge) = GetTokens();
            return new GeeTestV4ProxylessRequest()
            {
                WebsiteUrl = "http://www.supremenewyork.com",
                Gt = websiteKey,
                Challenge = websiteChallenge
            };
        }

        protected override void AssertTaskResult(TaskResultResponse<GeeTestV4Solution> taskResult)
        {
            AssertHelper.NotNullNotEmpty(taskResult.Solution.CaptchaId);
            AssertHelper.NotNullNotEmpty(taskResult.Solution.LotNumber);
            AssertHelper.NotNullNotEmpty(taskResult.Solution.PassToken);
            AssertHelper.NotNullNotEmpty(taskResult.Solution.GenTime);
            AssertHelper.NotNullNotEmpty(taskResult.Solution.CaptchaOutput);
        }
    }
}