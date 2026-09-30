using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>Body of <c>POST getSpendingStats</c>. Unset filters are null and therefore omitted.</summary>
internal sealed class GetSpendingStatsRequest
{
    public GetSpendingStatsRequest(string clientKey, SpendingQuery? query)
    {
        ClientKey = clientKey;
        Date = query?.Date?.ToUnixTimeSeconds();
        Queue = query?.Queue;
        SoftId = query?.SoftId;
        Ip = query?.Ip;
    }

    [JsonPropertyName("clientKey")]
    public string ClientKey { get; }

    [JsonPropertyName("date")]
    public long? Date { get; }

    [JsonPropertyName("queue")]
    public string? Queue { get; }

    [JsonPropertyName("softId")]
    public int? SoftId { get; }

    [JsonPropertyName("ip")]
    public string? Ip { get; }
}
