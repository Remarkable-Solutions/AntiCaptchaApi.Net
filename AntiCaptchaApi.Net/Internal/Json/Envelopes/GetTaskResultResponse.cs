using System.Text.Json;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>
/// Success payload of <c>getTaskResult</c>. The solution stays raw until the caller's solution type is known.
/// <c>cost</c> and <c>solveCount</c> arrive as strings in the documented examples, so numbers are read from strings too;
/// <c>createTime</c>/<c>endTime</c> are unix seconds.
/// </summary>
internal sealed class GetTaskResultResponse
{
    public const string Processing = "processing";
    public const string Ready = "ready";

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("solution")]
    public JsonElement? Solution { get; init; }

    [JsonPropertyName("cost")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? Cost { get; init; }

    [JsonPropertyName("ip")]
    public string? Ip { get; init; }

    [JsonPropertyName("createTime")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long? CreateTime { get; init; }

    [JsonPropertyName("endTime")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long? EndTime { get; init; }

    [JsonPropertyName("solveCount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? SolveCount { get; init; }
}
