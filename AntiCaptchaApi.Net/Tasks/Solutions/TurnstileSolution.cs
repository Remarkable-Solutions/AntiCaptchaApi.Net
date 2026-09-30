using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>The solution of a <see cref="TurnstileTask"/>.</summary>
/// <param name="Token">The Turnstile token to submit with the protected form (<c>token</c>).</param>
/// <param name="UserAgent">The browser user agent the worker used, when the API returns it (<c>userAgent</c>).</param>
public sealed record TurnstileSolution(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("userAgent")] string? UserAgent) : Solution;
