using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>The solution of an <see cref="ImageToCoordinatesTask"/>.</summary>
/// <param name="Coordinates">The marked objects (<c>coordinates</c>): <c>[x, y]</c> per point, or
/// <c>[x1, y1, x2, y2]</c> per rectangle, in image pixels.</param>
public sealed record ImageToCoordinatesSolution(
    [property: JsonPropertyName("coordinates")] IReadOnlyList<IReadOnlyList<int>> Coordinates) : Solution;
