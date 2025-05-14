using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Helpers
{
    public static class BaseTaskResultResponseBuilder
    {
        public static TaskResultResponse<TSolution> Build<TSolution>(string errorCode, string errorMessage) 
            where TSolution : BaseSolution, new()
        {
            return new TaskResultResponse<TSolution>()
            {
                ErrorCode = errorCode,
                ErrorDescription = errorMessage,
                Solution = new TSolution()
            };
        }
    }
}