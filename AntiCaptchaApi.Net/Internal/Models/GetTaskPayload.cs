using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Models;

internal class GetTaskPayload<TSolution>(string clientKey, int taskId)
    : ClientPayload<TaskResultResponse<TSolution>>(clientKey)
    where TSolution : BaseSolution, new()
{
    public int TaskId { get; } = taskId;
}