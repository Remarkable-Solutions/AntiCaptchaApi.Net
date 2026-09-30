using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// GeeTest v3 (<c>GeeTestTaskProxyless</c> / <c>GeeTestTask</c> with <c>version</c> 3). A proxy is optional; with a
/// proxy, <see cref="UserAgent"/> is required.
/// See <see href="https://anti-captcha.com/apidoc/task-types/GeeTestTaskProxyless"/>.
/// </summary>
/// <param name="WebsiteUrl">The absolute URL of the page with the widget (<c>websiteURL</c>).</param>
/// <param name="Gt">The GeeTest <c>gt</c> key of the site (<c>gt</c>).</param>
/// <param name="Challenge">The <c>challenge</c> value, which changes with every page load (<c>challenge</c>).</param>
public sealed record GeeTestV3Task(
    [property: JsonPropertyName("websiteURL")] string WebsiteUrl,
    [property: JsonPropertyName("gt")] string Gt,
    [property: JsonPropertyName("challenge")] string Challenge) : CaptchaTask<GeeTestV3Solution>
{
    /// <summary>The GeeTest API version; always 3 (<c>version</c>).</summary>
    [JsonPropertyName("version")]
    public int Version => 3;

    /// <summary>A custom GeeTest API host, such as <c>api-na.geetest.com</c> (<c>geetestApiServerSubdomain</c>).</summary>
    [JsonPropertyName("geetestApiServerSubdomain")]
    public string? GeetestApiServerSubdomain { get; init; }

    /// <summary>The <c>getLib</c> parameter of the widget, as a JSON string (<c>geetestGetLib</c>).</summary>
    [JsonPropertyName("geetestGetLib")]
    public string? GeetestGetLib { get; init; }

    /// <summary>The browser user agent the worker should use (<c>userAgent</c>). Required when <see cref="CaptchaTask.Proxy"/> is set.</summary>
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; init; }

    /// <inheritdoc />
    protected override string TypeBase => "GeeTestTask";

    /// <inheritdoc />
    protected override void Validate(Validator v) =>
        v.Url(WebsiteUrl).NotEmpty(Gt).NotEmpty(Challenge).UserAgentWhenProxy(Proxy, UserAgent);
}
