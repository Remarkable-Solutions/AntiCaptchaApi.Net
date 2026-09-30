using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>The error fields that every anti-captcha API response carries. <c>errorId</c> 0 means success.</summary>
internal sealed class ApiErrorEnvelope
{
    [JsonPropertyName("errorId")]
    public int ErrorId { get; init; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    [JsonPropertyName("errorDescription")]
    public string? ErrorDescription { get; init; }
}
