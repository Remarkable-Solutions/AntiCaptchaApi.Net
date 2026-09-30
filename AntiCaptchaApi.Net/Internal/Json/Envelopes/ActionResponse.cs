using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>
/// Success payload of the report calls and <c>pushAntiGateVariable</c>: <c>{"errorId":0,"status":"success"}</c>.
/// Failures arrive as error envelopes, so <c>status</c> is read but not checked.
/// </summary>
internal sealed class ActionResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; init; }
}
