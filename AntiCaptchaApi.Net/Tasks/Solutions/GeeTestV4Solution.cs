using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>The solution of a <see cref="GeeTestV4Task"/>; submit the five values as the widget would.</summary>
/// <param name="CaptchaId">The captcha id (<c>captcha_id</c>).</param>
/// <param name="LotNumber">The lot number (<c>lot_number</c>).</param>
/// <param name="PassToken">The pass token (<c>pass_token</c>).</param>
/// <param name="GenTime">The generation time (<c>gen_time</c>).</param>
/// <param name="CaptchaOutput">The captcha output (<c>captcha_output</c>).</param>
public sealed record GeeTestV4Solution(
    [property: JsonPropertyName("captcha_id")] string CaptchaId,
    [property: JsonPropertyName("lot_number")] string LotNumber,
    [property: JsonPropertyName("pass_token")] string PassToken,
    [property: JsonPropertyName("gen_time")] string GenTime,
    [property: JsonPropertyName("captcha_output")] string CaptchaOutput) : Solution;
