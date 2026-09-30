using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// Arkose Labs FunCaptcha (<c>FunCaptchaTaskProxyless</c> / <c>FunCaptchaTask</c>). A proxy is optional; with a
/// proxy, <see cref="UserAgent"/> is required.
/// See <see href="https://anti-captcha.com/apidoc/task-types/FunCaptchaTaskProxyless"/>.
/// </summary>
/// <param name="WebsiteUrl">The absolute URL of the page with the widget (<c>websiteURL</c>).</param>
/// <param name="WebsitePublicKey">The FunCaptcha public key (<c>websitePublicKey</c>).</param>
public sealed record FunCaptchaTask(
    [property: JsonPropertyName("websiteURL")] string WebsiteUrl,
    [property: JsonPropertyName("websitePublicKey")] string WebsitePublicKey) : CaptchaTask<FunCaptchaSolution>
{
    /// <summary>A custom Arkose API host, such as <c>client-api.arkoselabs.com</c> (<c>funcaptchaApiJSSubdomain</c>).</summary>
    [JsonPropertyName("funcaptchaApiJSSubdomain")]
    public string? ApiJsSubdomain { get; init; }

    /// <summary>Extra widget data as a JSON string, for example <c>{"blob":"…"}</c> (<c>data</c>).</summary>
    [JsonPropertyName("data")]
    public string? Data { get; init; }

    /// <summary>The browser user agent the worker should use (<c>userAgent</c>). Required when <see cref="CaptchaTask.Proxy"/> is set.</summary>
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; init; }

    /// <inheritdoc />
    protected override string TypeBase => "FunCaptchaTask";

    /// <inheritdoc />
    protected override void Validate(Validator v) =>
        v.Url(WebsiteUrl).NotEmpty(WebsitePublicKey).UserAgentWhenProxy(Proxy, UserAgent);
}
