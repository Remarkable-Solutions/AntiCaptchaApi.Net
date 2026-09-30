using System;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// Image coordinates: the worker marks up to 6 points or rectangles on an image (<c>ImageToCoordinatesTask</c>).
/// No proxy is used. See <see href="https://anti-captcha.com/apidoc/task-types/ImageToCoordinatesTask"/>.
/// </summary>
/// <param name="Body">The image as plain base64, without line breaks or a <c>data:</c> prefix (<c>body</c>).
/// Use <see cref="FromFile"/> or <see cref="FromBytes"/> to encode an image.</param>
public sealed record ImageToCoordinatesTask(
    [property: JsonPropertyName("body")] string Body) : CaptchaTask<ImageToCoordinatesSolution>
{
    /// <summary>An instruction for the worker in English, such as <c>select all cars</c> (<c>comment</c>).</summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    /// <summary>Whether the worker marks points or rectangles (<c>mode</c>). Always sent; defaults to points.</summary>
    [JsonPropertyName("mode")]
    public ImageToCoordinatesMode Mode { get; init; } = ImageToCoordinatesMode.Points;

    /// <summary>The page the image comes from, used only to group spending statistics (<c>websiteURL</c>).</summary>
    [JsonPropertyName("websiteURL")]
    public string? WebsiteUrl { get; init; }

    /// <inheritdoc />
    protected override string TypeBase => "ImageToCoordinatesTask";

    /// <inheritdoc />
    protected override ProxyMode ProxyMode => ProxyMode.None;

    /// <summary>Image tasks are usually solved within seconds, so the first poll comes after 3 seconds.</summary>
    protected override TimeSpan FirstPollDelay => TimeSpan.FromSeconds(3);

    /// <summary>Creates a task from an image file.</summary>
    /// <param name="path">The path of the image file.</param>
    /// <returns>A task whose <see cref="Body"/> is the base64 content of the file.</returns>
    /// <exception cref="System.IO.FileNotFoundException">The file does not exist.</exception>
    public static ImageToCoordinatesTask FromFile(string path) => new(ImageBody.FromFile(path));

    /// <summary>Creates a task from the bytes of an image.</summary>
    /// <param name="image">The image bytes.</param>
    /// <returns>A task whose <see cref="Body"/> is the base64 encoding of <paramref name="image"/>.</returns>
    public static ImageToCoordinatesTask FromBytes(byte[] image) => new(ImageBody.FromBytes(image));

    /// <inheritdoc />
    protected override void Validate(Validator v)
    {
        ImageBody.Validate(v, Body);
        if (WebsiteUrl is not null)
        {
            v.Url(WebsiteUrl);
        }
    }
}
