using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>The solution of a <see cref="GeeTestV3Task"/>; submit the three values as the widget would.</summary>
/// <param name="Challenge">The solved challenge (<c>challenge</c>).</param>
/// <param name="Seccode">The security code (<c>seccode</c>).</param>
/// <param name="Validate">The validation value (<c>validate</c>).</param>
public sealed record GeeTestV3Solution(
    [property: JsonPropertyName("challenge")] string Challenge,
    [property: JsonPropertyName("seccode")] string Seccode,
    [property: JsonPropertyName("validate")] string Validate) : Solution;
