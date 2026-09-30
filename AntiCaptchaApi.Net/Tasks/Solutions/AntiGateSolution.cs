using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// The solution of an <see cref="AntiGateTask"/>: the state of the worker's browser when the template finished.
/// Only <see cref="Url"/> and <see cref="Domain"/> are always present; the other members depend on the template.
/// They are <see cref="JsonNode"/>s because the API sends either shape: maps are objects but an empty map arrives as
/// <c>[]</c>, and <c>HTMLsInBase64</c>, <c>screenshots</c> and the headers are documented as arrays.
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
    [property: JsonPropertyName("cookies")] JsonNode? Cookies,
    [property: JsonPropertyName("localStorage")] JsonNode? LocalStorage,
    [property: JsonPropertyName("sessionStorage")] JsonNode? SessionStorage,
    [property: JsonPropertyName("fingerprint")] JsonNode? Fingerprint,
    [property: JsonPropertyName("HTMLsInBase64")] JsonNode? HtmlsInBase64,
    [property: JsonPropertyName("screenshots")] JsonNode? Screenshots,
    [property: JsonPropertyName("requestHeaders")] JsonNode? RequestHeaders,
    [property: JsonPropertyName("responseHeaders")] JsonNode? ResponseHeaders) : Solution;
