using System;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Internal.Helpers;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.Helpers;
using AntiCaptchaApi.Net.Tests.IntegrationTests.Base;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.IntegrationTests.AnticaptchaRequests
{
    public class ImageToCoordinatesRequestTests : AnticaptchaRequestTestBase<ImageToCoordinatesSolution>
    {
        [Theory]
        [InlineData(ImageToCoordinatesMode.Points)]
        [InlineData(ImageToCoordinatesMode.Rectangles)]
        public async Task ShouldReturnCorrectCaptchaResult_WhenCallingAuthenticRequest(ImageToCoordinatesMode mode)
        {
            var captchaRequest = CreateAuthenticRequest();
            captchaRequest.Mode = mode;
            
            var (createTaskResponse, taskResult) = await TestCaptchaRequestAsync(captchaRequest);
            AssertHelper.Assert(createTaskResponse);
            AssertHelper.Assert(taskResult);
            AssertTaskResult(taskResult);

            var solution = taskResult.Solution;
        
            // Should be two cats on the image
            Assert.Equal(2, solution.Coordinates.Count);
            var firstCarCoordinates = solution.Coordinates[0];
            var secondCarCoordinates = solution.Coordinates[1];

            switch (mode)
            {
                case ImageToCoordinatesMode.Rectangles:
                    Assert.Equal(4, firstCarCoordinates.Count);
                    Assert.Equal(4, secondCarCoordinates.Count);
                    break;
                case ImageToCoordinatesMode.Points:
                    Assert.Equal(2, firstCarCoordinates.Count);
                    Assert.Equal(2, secondCarCoordinates.Count);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        protected override ImageToCoordinatesRequest CreateAuthenticRequest()
        {
            return new ImageToCoordinatesRequest
            {
                Body = StringHelper.ImageFileToBase64String("Resources\\cats.jpeg"),
                Comment = "Select all cats"
            };
        }

        protected override void AssertTaskResult(TaskResultResponse<ImageToCoordinatesSolution> taskResult)
        {
            Assert.NotEmpty(taskResult.Solution.Coordinates);
        }
    }
}