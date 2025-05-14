using System.Threading.Tasks;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.Helpers;
using AntiCaptchaApi.Net.Tests.IntegrationTests.Base;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.IntegrationTests.AnticaptchaRequests
{
    public class ProsopoRequestTests : AnticaptchaRequestTestBase<ProsopoSolution>
    {
        [Fact(Skip = "Find captcha website")]
        public async Task ShouldReturnCorrectCaptchaResult_WhenCallingAuthenticRequest()
        {
            if (!TestEnvironment.IsProxyDefined) // Skip if proxy is not configured
            {
                // No standard Assert.Skip available or used in this project context for dynamic skips.
                // Simply returning will make the test pass without executing the main logic.
                // For a clearer indication in test reports, a dedicated skip mechanism would be better if available.
                return; 
            }
            await TestAuthenticRequest();
        }

        protected override ProsopoRequest CreateAuthenticRequest()
        {
            // Note: Using example key from documentation. 
            // A live Prosopo test page URL would be ideal for a more comprehensive "authentic" test.
            return new ProsopoRequest
            {
                WebsiteUrl = "https://example-service-that-uses-prosopo.com", // Placeholder URL
                WebsiteKey = "5FxMg5jAF3F8d8PrQezDMZh6ZbZd69kDt6FUVb1KaFpSgS2l", // Example key from AntiCaptcha docs
                UserAgent = TestEnvironment.UserAgent, // Using UserAgent from TestEnvironment for consistency
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
}