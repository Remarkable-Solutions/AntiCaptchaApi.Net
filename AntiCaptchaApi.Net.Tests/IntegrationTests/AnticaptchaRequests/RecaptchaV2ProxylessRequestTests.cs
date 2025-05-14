using System.Threading.Tasks;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.Helpers;
using AntiCaptchaApi.Net.Tests.IntegrationTests.Base;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.IntegrationTests.AnticaptchaRequests
{
    public class RecaptchaV2ProxylessRequestTests : AnticaptchaRequestTestBase<RecaptchaSolution>
    {
        [Fact]
        public async Task ShouldReturnCorrectCaptchaResult_WhenCallingAuthenticRequest()
        {
            await TestAuthenticRequest();
        }

        protected override RecaptchaV2EnterpriseProxylessRequest CreateAuthenticRequest()
        {
            return new RecaptchaV2EnterpriseProxylessRequest()
            {
                WebsiteUrl = "http://http.myjino.ru/recaptcha/test-get.php",
                WebsiteKey = "6Lc_aCMTAAAAABx7u2W0WPXnVbI_v6ZdbM6rYf16"
            };
        }

        protected override void AssertTaskResult(TaskResultResponse<RecaptchaSolution> taskResult)
        {
            AssertHelper.NotNullNotEmpty(taskResult.Solution.GRecaptchaResponse);
        }
    }
}