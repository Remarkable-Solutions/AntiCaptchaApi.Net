using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Models
{
    internal class ActionPayload : ClientPayload<ActionResponse>
    {
        public ActionPayload(string clientKey, int taskId) : base(clientKey)
        {
            TaskId = taskId;
        }

        public int TaskId { get; }
    }
}