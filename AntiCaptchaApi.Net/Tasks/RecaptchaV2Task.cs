using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// Google reCAPTCHA v2, including the invisible variant (<c>RecaptchaV2TaskProxyless</c> / <c>RecaptchaV2Task</c>).
/// A proxy is optional; with a proxy, <see cref="UserAgent"/> is required.
/// See <see href="https://anti-captcha.com/apidoc/task-types/RecaptchaV2TaskProxyless"/>.
/// </summary>
/// <param name="WebsiteUrl">The absolute URL of the page with the widget (<c>websiteURL</c>).</param>
/// <param name="WebsiteKey">The reCAPTCHA site key (<c>websiteKey</c>).</param>
public sealed record RecaptchaV2Task(
    [property: JsonPropertyName("websiteURL")] string WebsiteUrl,
    [property: JsonPropertyName("websiteKey")] string WebsiteKey) : CaptchaTask<RecaptchaSolution>
{
    /// <summary>The <c>data-s</c> value of the widget, used by some Google services (<c>recaptchaDataSValue</c>).</summary>
    [JsonPropertyName("recaptchaDataSValue")]
    public string? RecaptchaDataSValue { get; init; }

    /// <summary><see langword="true"/> for invisible reCAPTCHA (<c>isInvisible</c>).</summary>
    [JsonPropertyName("isInvisible")]
    public bool? IsInvisible { get; init; }

    /// <summary>The browser user agent the worker should use (<c>userAgent</c>). Required when <see cref="CaptchaTask.Proxy"/> is set.</summary>
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; init; }

    /// <summary>Cookies for the page, as <c>name1=value1; name2=value2</c> (<c>cookies</c>).</summary>
    [JsonPropertyName("cookies")]
    public string? Cookies { get; init; }

    /// <inheritdoc />
    protected override string TypeBase => "RecaptchaV2Task";

    /// <inheritdoc />
    protected override void Validate(Validator v) =>
        v.Url(WebsiteUrl).NotEmpty(WebsiteKey).UserAgentWhenProxy(Proxy, UserAgent);
}
