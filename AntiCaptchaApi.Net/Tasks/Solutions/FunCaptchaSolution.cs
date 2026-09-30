using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>The solution of a <see cref="FunCaptchaTask"/>.</summary>
/// <param name="Token">The FunCaptcha token to submit with the protected form (<c>token</c>).</param>
public sealed record FunCaptchaSolution(
    [property: JsonPropertyName("token")] string Token) : Solution;
