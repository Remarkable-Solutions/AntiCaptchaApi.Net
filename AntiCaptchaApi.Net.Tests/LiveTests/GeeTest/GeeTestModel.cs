using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tests.LiveTests.GeeTest;

/// <summary>The GeeTest demo <c>init_captcha</c> response (ported from the 2.x Newtonsoft model to System.Text.Json).</summary>
internal sealed class GeeTestModel
{
    [JsonPropertyName("data")]
    public GeeTestData? Data { get; set; }
}
