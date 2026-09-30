using System.Text.Json;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>Success payload of <c>getAppStats</c>. <c>chartData</c> stays raw (documented as an array; 2.x also accepted an object).</summary>
internal sealed class GetAppStatsResponse
{
    [JsonPropertyName("chartData")]
    public JsonElement? ChartData { get; init; }

    [JsonPropertyName("fromDate")]
    public string? FromDate { get; init; }

    [JsonPropertyName("toDate")]
    public string? ToDate { get; init; }
}
