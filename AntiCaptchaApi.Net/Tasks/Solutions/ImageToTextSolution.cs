using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>The solution of an <see cref="ImageToTextTask"/>.</summary>
/// <param name="Text">The text in the image (<c>text</c>).</param>
/// <param name="Url">The URL of the stored image, when the API returns it (<c>url</c>).</param>
public sealed record ImageToTextSolution(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("url")] string? Url) : Solution;
