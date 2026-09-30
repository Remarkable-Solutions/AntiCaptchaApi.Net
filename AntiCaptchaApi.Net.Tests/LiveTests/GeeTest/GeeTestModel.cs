using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tests.LiveTests.GeeTest;

/// <summary>The GeeTest demo <c>init_captcha</c> response (the 2.x test model, ported to System.Text.Json).</summary>
internal sealed class GeeTestModel
{
    [JsonPropertyName("data")]
    public GeeTestData? Data { get; set; }
}
