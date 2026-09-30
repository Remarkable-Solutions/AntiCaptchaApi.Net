using System.Text.Json.Serialization;
using AntiCaptchaApi.Net.Internal.Json;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>What an <see cref="ImageToCoordinatesTask"/> worker marks. Sent as <c>points</c> or <c>rectangles</c>.</summary>
[JsonConverter(typeof(LowerCaseEnumConverter<ImageToCoordinatesMode>))]
public enum ImageToCoordinatesMode
{
    /// <summary>Single points, each as <c>[x, y]</c> (<c>points</c>, the default).</summary>
    Points,

    /// <summary>Rectangles, each as <c>[x1, y1, x2, y2]</c> (<c>rectangles</c>).</summary>
    Rectangles,
}
