using System;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// Google reCAPTCHA v3, including Enterprise v3 when <see cref="IsEnterprise"/> is set (<c>RecaptchaV3TaskProxyless</c>).
/// No proxy is used. See <see href="https://anti-captcha.com/apidoc/task-types/RecaptchaV3TaskProxyless"/>.
/// </summary>
/// <param name="WebsiteUrl">The absolute URL of the page that runs reCAPTCHA (<c>websiteURL</c>).</param>
/// <param name="WebsiteKey">The reCAPTCHA site key (<c>websiteKey</c>).</param>
public sealed record RecaptchaV3Task(
    [property: JsonPropertyName("websiteURL")] string WebsiteUrl,
    [property: JsonPropertyName("websiteKey")] string WebsiteKey) : CaptchaTask<RecaptchaSolution>
{
    /// <summary>The minimum score the token must reach (<c>minScore</c>). Always sent; defaults to <see cref="MinScore.Score03"/>.</summary>
    [JsonPropertyName("minScore")]
    public MinScore MinScore { get; init; } = MinScore.Score03;

    /// <summary>The action name the page passes to <c>grecaptcha.execute</c> (<c>pageAction</c>).</summary>
    [JsonPropertyName("pageAction")]
    public string? PageAction { get; init; }

    /// <summary><see langword="true"/> for reCAPTCHA Enterprise v3 (<c>isEnterprise</c>). Always sent; defaults to <see langword="false"/>.</summary>
    [JsonPropertyName("isEnterprise")]
    public bool IsEnterprise { get; init; }

    /// <summary>The domain the script is loaded from: <c>www.google.com</c> or <c>www.recaptcha.net</c> (<c>apiDomain</c>).</summary>
    [JsonPropertyName("apiDomain")]
    public string? ApiDomain { get; init; }

    /// <inheritdoc />
    protected override string TypeBase => "RecaptchaV3TaskProxyless";

    /// <inheritdoc />
    protected override ProxyMode ProxyMode => ProxyMode.None;

    /// <inheritdoc />
    protected override void Validate(Validator v)
    {
        v.Url(WebsiteUrl)
            .NotEmpty(WebsiteKey)
            .Must(Enum.IsDefined(typeof(MinScore), MinScore), nameof(MinScore), "must be a defined MinScore value");
        RecaptchaApiDomain.Validate(v, ApiDomain);
    }
}
