using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// Cloudflare Turnstile (<c>TurnstileTaskProxyless</c> / <c>TurnstileTask</c>). A proxy is optional; with a proxy,
/// <see cref="UserAgent"/> is required.
/// See <see href="https://anti-captcha.com/apidoc/task-types/TurnstileTaskProxyless"/>.
/// </summary>
/// <param name="WebsiteUrl">The absolute URL of the page with the widget (<c>websiteURL</c>).</param>
/// <param name="WebsiteKey">The Turnstile site key (<c>websiteKey</c>).</param>
public sealed record TurnstileTask(
    [property: JsonPropertyName("websiteURL")] string WebsiteUrl,
    [property: JsonPropertyName("websiteKey")] string WebsiteKey) : CaptchaTask<TurnstileSolution>
{
    /// <summary>The widget's <c>action</c> parameter, if the page sets one.</summary>
    [JsonPropertyName("action")]
    public string? Action { get; init; }

    /// <summary>The widget's <c>cData</c> parameter, if the page sets one.</summary>
    [JsonPropertyName("cData")]
    public string? CData { get; init; }

    /// <summary>The <c>chlPageData</c> parameter of Cloudflare challenge pages, if present.</summary>
    [JsonPropertyName("chlPageData")]
    public string? ChlPageData { get; init; }

    /// <summary>The browser user agent the worker should use (<c>userAgent</c>). Required when <see cref="CaptchaTask.Proxy"/> is set.</summary>
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; init; }

    /// <inheritdoc />
    protected override string TypeBase => "TurnstileTask";

    /// <inheritdoc />
    protected override void Validate(Validator v)
    {
        v.Url(WebsiteUrl).NotEmpty(WebsiteKey);
        if (Proxy is not null)
        {
            v.Must(!string.IsNullOrWhiteSpace(UserAgent), nameof(UserAgent), "required when Proxy is set");
        }
    }
}
