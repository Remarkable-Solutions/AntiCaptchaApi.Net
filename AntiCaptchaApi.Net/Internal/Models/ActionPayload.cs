using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Models;

internal class ActionPayload(string clientKey, int taskId) : ClientPayload<ActionResponse>(clientKey)
{
    public int TaskId { get; } = taskId;
}