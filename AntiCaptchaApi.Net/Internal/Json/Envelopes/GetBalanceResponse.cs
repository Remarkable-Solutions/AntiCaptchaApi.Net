using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>Success payload of <c>getBalance</c>. <c>balance</c> is a number in USD; a string is accepted too.</summary>
internal sealed class GetBalanceResponse
{
    [JsonPropertyName("balance")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? Balance { get; init; }
}
