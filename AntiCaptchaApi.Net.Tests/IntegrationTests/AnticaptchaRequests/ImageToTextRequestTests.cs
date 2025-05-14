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
    public class ImageToTextRequestTests : AnticaptchaRequestTestBase<ImageToTextSolution>
    {
        private const string ExpectedCaptchaResult = "W68HP";

        private static ImageToTextRequest CreateImageToTextRequest(string filePath = "")
        {
            return new ImageToTextRequest
            {
                FilePath = filePath
            };
        }
        
        [Fact]
        public async Task ShouldReturnCorrectCaptchaResult_WhenCallingAuthenticRequest()
        {
            await TestAuthenticRequest();
        }

        [Fact]
        public async Task ShouldReturnFalseWithCorrectException_WhenCallingWithIncorrectFilePath()
        {
            var request = CreateImageToTextRequest(filePath: "dsa.png");
            var result = await AnticaptchaClient.CreateCaptchaTaskAsync(request);
            Assert.NotEmpty(result.ErrorDescription);
            Assert.Contains(nameof(ImageToTextRequest.BodyBase64), result.ErrorDescription);
        }

        protected override ImageToTextRequest CreateAuthenticRequest()
        {
            return new ImageToTextRequest
            {
                FilePath = "Resources\\captchaexample.png"
            };
        }

        protected override void AssertTaskResult(TaskResultResponse<ImageToTextSolution> taskResult)
        {
            AssertHelper.NotNullNotEmpty(taskResult.Solution.Url);
            AssertHelper.NotNullNotEmpty(taskResult.Solution.Text);
            Assert.Equal(ExpectedCaptchaResult, taskResult.Solution.Text);
        }
    }
}