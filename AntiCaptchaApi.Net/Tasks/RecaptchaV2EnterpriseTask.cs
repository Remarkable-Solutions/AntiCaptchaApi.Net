using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// Google reCAPTCHA v2 Enterprise (<c>RecaptchaV2EnterpriseTaskProxyless</c> / <c>RecaptchaV2EnterpriseTask</c>).
/// A proxy is optional; with a proxy, <see cref="UserAgent"/> is required.
/// See <see href="https://anti-captcha.com/apidoc/task-types/RecaptchaV2EnterpriseTaskProxyless"/>.
/// </summary>
/// <param name="WebsiteUrl">The absolute URL of the page with the widget (<c>websiteURL</c>).</param>
/// <param name="WebsiteKey">The reCAPTCHA site key (<c>websiteKey</c>).</param>
public sealed record RecaptchaV2EnterpriseTask(
    [property: JsonPropertyName("websiteURL")] string WebsiteUrl,
    [property: JsonPropertyName("websiteKey")] string WebsiteKey) : CaptchaTask<RecaptchaSolution>
{
    /// <summary>
    /// Extra parameters passed to <c>grecaptcha.enterprise.render</c>, such as <c>s</c>, sent as a JSON object
    /// (<c>enterprisePayload</c>).
    /// </summary>
    [JsonPropertyName("enterprisePayload")]
    public IReadOnlyDictionary<string, string>? EnterprisePayload { get; init; }

    /// <summary>The domain the widget is loaded from: <c>www.google.com</c> or <c>www.recaptcha.net</c> (<c>apiDomain</c>).</summary>
    [JsonPropertyName("apiDomain")]
    public string? ApiDomain { get; init; }

    /// <summary>The browser user agent the worker should use (<c>userAgent</c>). Required when <see cref="CaptchaTask.Proxy"/> is set.</summary>
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; init; }

    /// <summary>Cookies for the page, as <c>name1=value1; name2=value2</c> (<c>cookies</c>).</summary>
    [JsonPropertyName("cookies")]
    public string? Cookies { get; init; }

    /// <inheritdoc />
    protected override string TypeBase => "RecaptchaV2EnterpriseTask";

    /// <inheritdoc />
    protected override void Validate(Validator v)
    {
        v.Url(WebsiteUrl).NotEmpty(WebsiteKey).UserAgentWhenProxy(Proxy, UserAgent);
        RecaptchaApiDomain.Validate(v, ApiDomain);
    }
}
