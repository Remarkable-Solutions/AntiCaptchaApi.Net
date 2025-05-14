using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.Helpers;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.IntegrationTests.AnticaptchaRequests
{
    public class GeeRequestTestProxylessV3RequestTests : GeeRequestTestsBase<GeeTestV3Solution>
    {
        [Fact]
        public async Task ShouldReturnCorrectCaptchaResult_WhenCallingAuthenticRequest()
        {
            Assert.False(true, "Unsolvable request"); //TODO!
            await TestAuthenticRequest();
        }

        protected override GeeTestV3ProxylessRequest CreateAuthenticRequest()
        {
            return new GeeTestV3ProxylessRequest()
            {
                WebsiteUrl = "https://www.seloger.com/",
                Gt = "1e505deed3832c02c96ca5abe70df9ab",
                Challenge = "1ea0aba2104e7366ba764f86527df09a"
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