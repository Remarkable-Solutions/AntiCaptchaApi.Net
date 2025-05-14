using System.Threading.Tasks;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.Helpers;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.IntegrationTests.Base
{
    public abstract class AnticaptchaRequestTestBase <TSolution> : AnticaptchaTestBase
        where TSolution : BaseSolution, new()
    {
        protected abstract CaptchaRequest<TSolution> CreateAuthenticRequest();
        protected abstract void AssertTaskResult(TaskResultResponse<TSolution> taskResult);

        protected async Task<(CreateTaskResponse creationTaskResult, TaskResultResponse<TSolution> taskResult)> TestCaptchaRequestAsync(CaptchaRequest<TSolution> captchaRequest)
        {
            var creationTaskResult = await AnticaptchaClient.CreateCaptchaTaskAsync(captchaRequest);
            AssertHelper.Assert(creationTaskResult);
            Assert.NotNull(creationTaskResult.TaskId);
            var taskResult = await AnticaptchaClient.WaitForTaskResultAsync<TSolution>(creationTaskResult.TaskId!.Value);
            AssertHelper.Assert(taskResult);
            return (creationTaskResult, taskResult);
        }

        protected async Task TestAuthenticRequest()
        {
            if (!TestEnvironment.IsProxyDefined)
                Assert.True(TestEnvironment.IsProxyDefined);
        
            var captchaRequest = CreateAuthenticRequest();
            
            var (createTaskResponse, taskResult) = await TestCaptchaRequestAsync(captchaRequest);
            AssertHelper.Assert(createTaskResponse);
            AssertHelper.Assert(taskResult);
            AssertTaskResult(taskResult);
        }
    }
}