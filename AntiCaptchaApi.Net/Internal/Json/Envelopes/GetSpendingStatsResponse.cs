using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>
/// Success payload of <c>getSpendingStats</c>: the records are in <c>data[]</c> (2.x read a non-existent
/// <c>AllDailySpendingStats</c>; spec §9 item 5). Times are unix seconds; numbers are also read from strings.
/// </summary>
internal sealed class GetSpendingStatsResponse
{
    [JsonPropertyName("data")]
    public List<Entry?>? Data { get; init; }

    internal sealed class Entry
    {
        [JsonPropertyName("dateFrom")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? DateFrom { get; init; }

        [JsonPropertyName("dateTill")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? DateTill { get; init; }

        [JsonPropertyName("volume")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? Volume { get; init; }

        [JsonPropertyName("money")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal? Money { get; init; }
    }
}
