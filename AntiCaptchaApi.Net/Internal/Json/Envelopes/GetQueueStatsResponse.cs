using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>
/// Success payload of <c>getQueueStats</c>. The documented example sends <c>bid</c> as a string, so every number is also
/// read from a string. The documented example has no <c>errorId</c>; a missing one reads as 0 (success).
/// </summary>
internal sealed class GetQueueStatsResponse
{
    [JsonPropertyName("waiting")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? Waiting { get; init; }

    [JsonPropertyName("load")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double? Load { get; init; }

    [JsonPropertyName("bid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? Bid { get; init; }

    [JsonPropertyName("speed")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double? Speed { get; init; }

    [JsonPropertyName("total")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? Total { get; init; }
}
