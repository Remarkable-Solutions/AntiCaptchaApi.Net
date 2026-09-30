using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// Friendly Captcha (<c>FriendlyCaptchaTaskProxyless</c> / <c>FriendlyCaptchaTask</c>). A proxy is optional; with a proxy,
/// <see cref="UserAgent"/> is required.
/// See <see href="https://anti-captcha.com/apidoc/task-types/FriendlyCaptchaTaskProxyless"/>.
/// </summary>
/// <param name="WebsiteUrl">The absolute URL of the page with the widget (<c>websiteURL</c>).</param>
/// <param name="WebsiteKey">The Friendly Captcha site key (<c>websiteKey</c>).</param>
public sealed record FriendlyCaptchaTask(
    [property: JsonPropertyName("websiteURL")] string WebsiteUrl,
    [property: JsonPropertyName("websiteKey")] string WebsiteKey) : CaptchaTask<FriendlyCaptchaSolution>
{
    /// <summary>The browser user agent the worker should use (<c>userAgent</c>). Required when <see cref="CaptchaTask.Proxy"/> is set.</summary>
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; init; }

    /// <inheritdoc />
    protected override string TypeBase => "FriendlyCaptchaTask";

    /// <inheritdoc />
    protected override void Validate(Validator v) =>
        v.Url(WebsiteUrl).NotEmpty(WebsiteKey).UserAgentWhenProxy(Proxy, UserAgent);
}
