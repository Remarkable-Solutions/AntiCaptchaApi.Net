using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// Amazon AWS WAF captcha (<c>AmazonTaskProxyless</c> / <c>AmazonTask</c>). A proxy is optional.
/// See <see href="https://anti-captcha.com/apidoc/task-types/AmazonTaskProxyless"/>.
/// </summary>
/// <param name="WebsiteUrl">The absolute URL of the protected page (<c>websiteURL</c>).</param>
/// <param name="WebsiteKey">The <c>key</c> value of <c>window.gokuProps</c> (<c>websiteKey</c>).</param>
/// <param name="Iv">The <c>iv</c> value of <c>window.gokuProps</c> (<c>iv</c>).</param>
/// <param name="Context">The <c>context</c> value of <c>window.gokuProps</c> (<c>context</c>).</param>
public sealed record AmazonWafTask(
    [property: JsonPropertyName("websiteURL")] string WebsiteUrl,
    [property: JsonPropertyName("websiteKey")] string WebsiteKey,
    [property: JsonPropertyName("iv")] string Iv,
    [property: JsonPropertyName("context")] string Context) : CaptchaTask<AmazonWafSolution>
{
    /// <summary>The URL of the <c>captcha.js</c> script of the page, if it has one (<c>captchaScript</c>).</summary>
    [JsonPropertyName("captchaScript")]
    public string? CaptchaScript { get; init; }

    /// <summary>The URL of the <c>challenge.js</c> script of the page, if it has one (<c>challengeScript</c>).</summary>
    [JsonPropertyName("challengeScript")]
    public string? ChallengeScript { get; init; }

    /// <inheritdoc />
    protected override string TypeBase => "AmazonTask";

    /// <summary>Checks the URL and the three <c>gokuProps</c> values, with or without a proxy (2.x checked Iv and Context only without one).</summary>
    /// <param name="v">The validator that collects the errors.</param>
    protected override void Validate(Validator v) =>
        v.Url(WebsiteUrl).NotEmpty(WebsiteKey).NotEmpty(Iv).NotEmpty(Context);
}
