using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// AntiGate: a worker opens <see cref="WebsiteUrl"/> in a real browser and runs the AntiGate template
/// <see cref="TemplateName"/> with <see cref="Variables"/> (<c>AntiGateTask</c>). The type name is the same with or
/// without a proxy. A proxy is optional; anti-captcha accepts only HTTPS proxies given by IP address for this type, and
/// <c>proxyType</c> is not sent. See <see href="https://anti-captcha.com/apidoc/task-types/AntiGateTask"/>.
/// </summary>
/// <param name="WebsiteUrl">The absolute URL the worker opens first (<c>websiteURL</c>).</param>
/// <param name="TemplateName">The name of a template from the AntiGate catalog, or of your private template (<c>templateName</c>).</param>
/// <param name="Variables">The template variables, sent unchanged as a JSON object (<c>variables</c>).</param>
public sealed record AntiGateTask(
    [property: JsonPropertyName("websiteURL")] string WebsiteUrl,
    [property: JsonPropertyName("templateName")] string TemplateName,
    [property: JsonPropertyName("variables")] JsonObject Variables) : CaptchaTask<AntiGateSolution>
{
    /// <summary>
    /// Extra domains whose cookies and storage are collected, in addition to the domain of the final page
    /// (<c>domainsOfInterest</c>).
    /// </summary>
    [JsonPropertyName("domainsOfInterest")]
    public IReadOnlyList<string>? DomainsOfInterest { get; init; }

    /// <inheritdoc />
    protected override string TypeBase => "AntiGateTask";

    /// <inheritdoc />
    protected override ProxyMode ProxyMode => ProxyMode.OptionalSameName;

    internal override bool IncludesProxyType => false;

    /// <inheritdoc />
    protected override void Validate(Validator v) =>
        v.Url(WebsiteUrl).NotEmpty(TemplateName).NotNull(Variables);
}
