using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// Prosopo Procaptcha (<c>ProsopoTaskProxyless</c> / <c>ProsopoTask</c>). A proxy is optional; with a proxy,
/// <see cref="UserAgent"/> is required.
/// See <see href="https://anti-captcha.com/apidoc/task-types/ProsopoTaskProxyless"/>.
/// </summary>
/// <param name="WebsiteUrl">The absolute URL of the page with the widget (<c>websiteURL</c>).</param>
/// <param name="WebsiteKey">The Prosopo site key (<c>websiteKey</c>).</param>
public sealed record ProsopoTask(
    [property: JsonPropertyName("websiteURL")] string WebsiteUrl,
    [property: JsonPropertyName("websiteKey")] string WebsiteKey) : CaptchaTask<ProsopoSolution>
{
    /// <summary>The browser user agent the worker should use (<c>userAgent</c>). Required when <see cref="CaptchaTask.Proxy"/> is set.</summary>
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; init; }

    /// <inheritdoc />
    protected override string TypeBase => "ProsopoTask";

    /// <inheritdoc />
    protected override void Validate(Validator v) =>
        v.Url(WebsiteUrl).NotEmpty(WebsiteKey).UserAgentWhenProxy(Proxy, UserAgent);
}
