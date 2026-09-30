using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>
/// Body of <c>POST getAppStats</c>. <see cref="SoftId"/> is the caller's app id (it selects whose stats to read) and is
/// unrelated to the library's own <c>createTask</c> softId. A null <see cref="Mode"/> is omitted.
/// </summary>
internal sealed class GetAppStatsRequest
{
    public GetAppStatsRequest(string clientKey, int softId, AppStatsMode? mode)
    {
        ClientKey = clientKey;
        SoftId = softId;
        Mode = mode;
    }

    [JsonPropertyName("clientKey")]
    public string ClientKey { get; }

    [JsonPropertyName("softId")]
    public int SoftId { get; }

    [JsonPropertyName("mode")]
    public AppStatsMode? Mode { get; }
}
