using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// GeeTest v4 (<c>GeeTestTaskProxyless</c> / <c>GeeTestTask</c> with <c>version</c> 4). A proxy is optional; with a
/// proxy, <see cref="UserAgent"/> is required.
/// See <see href="https://anti-captcha.com/apidoc/task-types/GeeTestTaskProxyless"/>.
/// </summary>
/// <param name="WebsiteUrl">The absolute URL of the page with the widget (<c>websiteURL</c>).</param>
/// <param name="Gt">The GeeTest v4 <c>captcha_id</c> of the site (<c>gt</c>).</param>
public sealed record GeeTestV4Task(
    [property: JsonPropertyName("websiteURL")] string WebsiteUrl,
    [property: JsonPropertyName("gt")] string Gt) : CaptchaTask<GeeTestV4Solution>
{
    /// <summary>The GeeTest API version; always 4 (<c>version</c>).</summary>
    [JsonPropertyName("version")]
    public int Version => 4;

    /// <summary>The <c>challenge</c> value, if the page provides one; optional for v4 (<c>challenge</c>).</summary>
    [JsonPropertyName("challenge")]
    public string? Challenge { get; init; }

    /// <summary>A custom GeeTest API host, such as <c>api-na.geetest.com</c> (<c>geetestApiServerSubdomain</c>).</summary>
    [JsonPropertyName("geetestApiServerSubdomain")]
    public string? GeetestApiServerSubdomain { get; init; }

    /// <summary>The <c>getLib</c> parameter of the widget, as a JSON string (<c>geetestGetLib</c>).</summary>
    [JsonPropertyName("geetestGetLib")]
    public string? GeetestGetLib { get; init; }

    /// <summary>Extra v4 initialization parameters, such as <c>riskType</c>, sent as a JSON object (<c>initParameters</c>).</summary>
    [JsonPropertyName("initParameters")]
    public IReadOnlyDictionary<string, string>? InitParameters { get; init; }

    /// <summary>The browser user agent the worker should use (<c>userAgent</c>). Required when <see cref="CaptchaTask.Proxy"/> is set.</summary>
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; init; }

    /// <inheritdoc />
    protected override string TypeBase => "GeeTestTask";

    /// <inheritdoc />
    protected override void Validate(Validator v) =>
        v.Url(WebsiteUrl).NotEmpty(Gt).UserAgentWhenProxy(Proxy, UserAgent);
}
