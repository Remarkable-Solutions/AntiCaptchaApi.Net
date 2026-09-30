using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>The solution of a <see cref="RecaptchaV2Task"/>, <see cref="RecaptchaV2EnterpriseTask"/> or <c>RecaptchaV3Task</c>.</summary>
/// <param name="GRecaptchaResponse">The token to put into <c>g-recaptcha-response</c> (<c>gRecaptchaResponse</c>).</param>
/// <param name="GRecaptchaResponseMd5">The MD5 hash of the token, when the API returns it (<c>gRecaptchaResponseMd5</c>).</param>
public sealed record RecaptchaSolution(
    [property: JsonPropertyName("gRecaptchaResponse")] string GRecaptchaResponse,
    [property: JsonPropertyName("gRecaptchaResponseMd5")] string? GRecaptchaResponseMd5) : Solution;
