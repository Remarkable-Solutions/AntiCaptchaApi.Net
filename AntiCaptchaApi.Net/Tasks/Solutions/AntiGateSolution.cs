using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// The solution of an <see cref="AntiGateTask"/>: the state of the worker's browser when the template finished.
/// Only <see cref="Url"/> and <see cref="Domain"/> are always present; the other members depend on the template.
/// </summary>
/// <param name="Url">The URL of the final page (<c>url</c>).</param>
/// <param name="Domain">The domain of the final page (<c>domain</c>).</param>
/// <param name="Cookies">The cookies, by name (<c>cookies</c>).</param>
/// <param name="LocalStorage">The <c>localStorage</c> entries (<c>localStorage</c>).</param>
/// <param name="SessionStorage">The <c>sessionStorage</c> entries (<c>sessionStorage</c>).</param>
/// <param name="Fingerprint">The browser fingerprint, such as <c>self.navigator.userAgent</c> (<c>fingerprint</c>).</param>
/// <param name="HtmlsInBase64">Page HTML captured by the template, base64-encoded (<c>HTMLsInBase64</c>).</param>
/// <param name="Screenshots">Screenshots captured by the template (<c>screenshots</c>).</param>
/// <param name="RequestHeaders">Request headers captured by the template (<c>requestHeaders</c>).</param>
/// <param name="ResponseHeaders">Response headers captured by the template (<c>responseHeaders</c>).</param>
public sealed record AntiGateSolution(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("domain")] string Domain,
    [property: JsonPropertyName("cookies")] JsonObject? Cookies,
    [property: JsonPropertyName("localStorage")] JsonObject? LocalStorage,
    [property: JsonPropertyName("sessionStorage")] JsonObject? SessionStorage,
    [property: JsonPropertyName("fingerprint")] JsonObject? Fingerprint,
    [property: JsonPropertyName("HTMLsInBase64")] JsonObject? HtmlsInBase64,
    [property: JsonPropertyName("screenshots")] JsonObject? Screenshots,
    [property: JsonPropertyName("requestHeaders")] JsonObject? RequestHeaders,
    [property: JsonPropertyName("responseHeaders")] JsonObject? ResponseHeaders) : Solution;
