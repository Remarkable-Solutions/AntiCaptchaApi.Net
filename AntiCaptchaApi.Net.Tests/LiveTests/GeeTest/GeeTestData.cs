using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tests.LiveTests.GeeTest;

/// <summary>The <c>gt</c>/<c>challenge</c> pair of the GeeTest demo page.</summary>
internal sealed class GeeTestData
{
    [JsonPropertyName("gt")]
    public string? Gt { get; set; }

    [JsonPropertyName("challenge")]
    public string? Challenge { get; set; }
}
