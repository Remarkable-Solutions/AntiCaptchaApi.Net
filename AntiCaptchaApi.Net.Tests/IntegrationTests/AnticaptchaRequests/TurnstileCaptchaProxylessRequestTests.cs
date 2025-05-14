using System.Threading.Tasks;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.Helpers;
using AntiCaptchaApi.Net.Tests.IntegrationTests.Base;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.IntegrationTests.AnticaptchaRequests
{
    public class TurnstileCaptchaProxylessRequestTests : AnticaptchaRequestTestBase<TurnstileSolution>
    {
        [Fact]
        public async Task ShouldReturnCorrectCaptchaResult_WhenCallingAuthenticRequest()
        {
            await TestAuthenticRequest();
        }

        protected override TurnstileCaptchaProxylessRequest CreateAuthenticRequest()
        {
            return new TurnstileCaptchaProxylessRequest()
            {
                WebsiteUrl = "https://react-turnstile.vercel.app/",
                WebsiteKey = "3x00000000000000000000FF",
            };
        }

        protected override void AssertTaskResult(TaskResultResponse<TurnstileSolution> taskResult)
        {
            AssertHelper.NotNullNotEmpty(taskResult.Solution.Token);
        }
    }
}