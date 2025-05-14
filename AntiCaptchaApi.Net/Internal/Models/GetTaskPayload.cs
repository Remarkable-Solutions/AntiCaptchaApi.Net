using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Models
{
    internal class GetTaskPayload<TSolution> : ClientPayload<TaskResultResponse<TSolution>>
        where TSolution : BaseSolution, new()
    {
        public GetTaskPayload(string clientKey, int taskId) : base(clientKey)
        {
            TaskId = taskId;
        }

        public int TaskId { get; }
    }
}