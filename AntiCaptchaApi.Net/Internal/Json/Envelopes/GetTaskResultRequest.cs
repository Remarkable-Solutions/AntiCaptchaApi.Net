using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>Body of <c>POST getTaskResult</c>.</summary>
internal sealed class GetTaskResultRequest
{
    public GetTaskResultRequest(string clientKey, int taskId)
    {
        ClientKey = clientKey;
        TaskId = taskId;
    }

    [JsonPropertyName("clientKey")]
    public string ClientKey { get; }

    [JsonPropertyName("taskId")]
    public int TaskId { get; }
}
