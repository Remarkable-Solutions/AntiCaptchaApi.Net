using System.Text.Json.Serialization;
using AntiCaptchaApi.Net.Internal.Json;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// The minimum reCAPTCHA v3 score a <see cref="RecaptchaV3Task"/> token must reach. These are the three worker
/// pools anti-captcha offers; each is sent as a number (<c>0.3</c>, <c>0.7</c>, <c>0.9</c>).
/// </summary>
[JsonConverter(typeof(MinScoreJsonConverter))]
public enum MinScore
{
    /// <summary>Score 0.3 (the default).</summary>
    Score03 = 0,

    /// <summary>Score 0.7.</summary>
    Score07 = 1,

    /// <summary>Score 0.9.</summary>
    Score09 = 2,
}
