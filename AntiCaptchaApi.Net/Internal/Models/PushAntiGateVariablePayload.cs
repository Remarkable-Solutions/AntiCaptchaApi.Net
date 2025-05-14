using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Models
{
    internal class PushAntiGateVariablePayload : ClientPayload<ActionResponse>
    {
        public PushAntiGateVariablePayload(string clientKey, int taskId, string name, object value) : base(clientKey)
        {
            TaskId = taskId;
            Name = name;
            Value = value;
        }

        public int TaskId { get; }
        public string Name { get; }
        public object Value { get; }
    }
}