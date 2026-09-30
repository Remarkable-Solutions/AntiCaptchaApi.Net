using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>Success payload of <c>createTask</c>. Error fields are read separately through <see cref="ApiErrorEnvelope"/>.</summary>
internal sealed class CreateTaskResponse
{
    [JsonPropertyName("taskId")]
    public int TaskId { get; init; }
}
