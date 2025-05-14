using System.Collections.Generic;
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
    public class RecaptchaV2EnterpriseProxylessRequestTests : AnticaptchaRequestTestBase <RecaptchaSolution>
    {
        [Fact]
        public async Task ShouldReturnCorrectCaptchaResult_WhenCallingAuthenticRequest()
        {
            await TestAuthenticRequest();
        }
        
        [Fact]
        public async Task ShouldReturnCorrectCaptchaResult_WhenCallingFactualAnticaptchaSolve()
        {
            var request = new RecaptchaV2EnterpriseProxylessRequest
            {
                WebsiteUrl = "https://store.steampowered.com/join",
                WebsiteKey = "6LdIFr0ZAAAAAO3vz0O0OQrtAefzdJcWQM2TMYQH",
                EnterprisePayload = new Dictionary<string, string>
                {
                    { "test", "qwerty" },
                    { "secret", "AB_12345" }
                }
            };

            var taskResult = await AnticaptchaClient.SolveCaptchaAsync(request);
            Assert.NotNull(taskResult);
            Assert.False(taskResult.CreateTaskResponse.IsErrorResponse);
            Assert.NotNull(taskResult.CreateTaskResponse);
            Assert.Null(taskResult.CreateTaskResponse.ErrorCode);
            AssertHelper.NotNullNotEmpty(taskResult.Solution.GRecaptchaResponse);
        }

        protected override RecaptchaV2EnterpriseProxylessRequest CreateAuthenticRequest()
        {
            var request = new RecaptchaV2EnterpriseProxylessRequest
            {
                WebsiteUrl = "https://store.steampowered.com/join",
                WebsiteKey = "6LdIFr0ZAAAAAO3vz0O0OQrtAefzdJcWQM2TMYQH",
                EnterprisePayload = new Dictionary<string, string>
                {
                    { "test", "qwerty" },
                    { "secret", "AB_12345" }
                }
            };

            return request;
        }

        protected override void AssertTaskResult(TaskResultResponse<RecaptchaSolution> taskResult)
        {
            AssertHelper.NotNullNotEmpty(taskResult.Solution.GRecaptchaResponse);
        }
    }
}