using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>Body of <c>POST pushAntiGateVariable</c>. <see cref="Value"/> is already converted by <see cref="AntiGateVariableValue"/>.</summary>
internal sealed class PushAntiGateVariableRequest
{
    public PushAntiGateVariableRequest(string clientKey, int taskId, string name, JsonNode value)
    {
        ClientKey = clientKey;
        TaskId = taskId;
        Name = name;
        Value = value;
    }

    [JsonPropertyName("clientKey")]
    public string ClientKey { get; }

    [JsonPropertyName("taskId")]
    public int TaskId { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("value")]
    public JsonNode Value { get; }
}
