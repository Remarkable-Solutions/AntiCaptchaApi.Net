using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>Body of <c>POST createTask</c>. <see cref="SoftId"/> is always the library constant and cannot be set.</summary>
internal sealed class CreateTaskRequest
{
    public CreateTaskRequest(string clientKey, JsonObject task)
    {
        ClientKey = clientKey;
        Task = task;
    }

    [JsonPropertyName("clientKey")]
    public string ClientKey { get; }

    [JsonPropertyName("task")]
    public JsonObject Task { get; }

    [JsonPropertyName("softId")]
    public int SoftId => global::AntiCaptchaApi.Net.Internal.SoftId.Value;

    [JsonPropertyName("languagePool")]
    public string? LanguagePool { get; init; }

    [JsonPropertyName("callbackUrl")]
    public string? CallbackUrl { get; init; }
}
