using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>Body of the calls on an existing task that need nothing else, for example <c>POST reportIncorrectRecaptcha</c>.</summary>
internal sealed class TaskActionRequest
{
    public TaskActionRequest(string clientKey, int taskId)
    {
        ClientKey = clientKey;
        TaskId = taskId;
    }

    [JsonPropertyName("clientKey")]
    public string ClientKey { get; }

    [JsonPropertyName("taskId")]
    public int TaskId { get; }
}
