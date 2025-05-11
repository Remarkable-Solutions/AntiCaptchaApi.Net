using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Models;

internal class PushAntiGateVariablePayload(string clientKey, int taskId, string name, object value)
    : ClientPayload<ActionResponse>(clientKey)
{
    public int TaskId { get; } = taskId;
    public string Name { get; } = name;
    public object Value { get; } = value;
}