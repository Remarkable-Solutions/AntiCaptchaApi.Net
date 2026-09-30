using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>The solution of an <see cref="AmazonWafTask"/>.</summary>
/// <param name="Token">The AWS WAF token, to set as the <c>aws-waf-token</c> cookie (<c>token</c>).</param>
public sealed record AmazonWafSolution(
    [property: JsonPropertyName("token")] string Token) : Solution;
