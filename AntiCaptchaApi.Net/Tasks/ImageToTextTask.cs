using System;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// Image captcha: the worker types the text shown in an image (<c>ImageToTextTask</c>). No proxy is used.
/// See <see href="https://anti-captcha.com/apidoc/task-types/ImageToTextTask"/>.
/// </summary>
/// <param name="Body">The image as plain base64, without line breaks or a <c>data:</c> prefix (<c>body</c>).
/// Use <see cref="FromFile"/> or <see cref="FromBytes"/> to encode an image.</param>
public sealed record ImageToTextTask(
    [property: JsonPropertyName("body")] string Body) : CaptchaTask<ImageToTextSolution>
{
    /// <summary>When <see langword="true"/>, the answer must contain at least one space (<c>phrase</c>).</summary>
    [JsonPropertyName("phrase")]
    public bool? Phrase { get; init; }

    /// <summary>When <see langword="true"/>, the answer is case-sensitive (<c>case</c>).</summary>
    [JsonPropertyName("case")]
    public bool? Case { get; init; }

    /// <summary>Which characters the answer may contain (<c>numeric</c>).</summary>
    [JsonPropertyName("numeric")]
    public NumericOption? Numeric { get; init; }

    /// <summary>When <see langword="true"/>, the worker must calculate the answer, for example <c>2 + 3</c> (<c>math</c>).</summary>
    [JsonPropertyName("math")]
    public bool? Math { get; init; }

    /// <summary>The minimum answer length; <c>0</c> means no requirement (<c>minLength</c>).</summary>
    [JsonPropertyName("minLength")]
    public int? MinLength { get; init; }

    /// <summary>The maximum answer length; <c>0</c> means no requirement (<c>maxLength</c>).</summary>
    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; init; }

    /// <summary>An instruction for the worker, such as <c>enter red text only</c> (<c>comment</c>).</summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    /// <summary>The page the image comes from, used only to group spending statistics (<c>websiteURL</c>).</summary>
    [JsonPropertyName("websiteURL")]
    public string? WebsiteUrl { get; init; }

    /// <inheritdoc />
    protected override string TypeBase => "ImageToTextTask";

    /// <inheritdoc />
    protected override ProxyMode ProxyMode => ProxyMode.None;

    /// <summary>Image tasks are usually solved within seconds, so the first poll comes after 3 seconds.</summary>
    protected override TimeSpan FirstPollDelay => TimeSpan.FromSeconds(3);

    /// <summary>Creates a task from an image file.</summary>
    /// <param name="path">The path of the image file.</param>
    /// <returns>A task whose <see cref="Body"/> is the base64 content of the file.</returns>
    /// <exception cref="System.IO.FileNotFoundException">The file does not exist.</exception>
    public static ImageToTextTask FromFile(string path) => new(ImageBody.FromFile(path));

    /// <summary>Creates a task from the bytes of an image.</summary>
    /// <param name="image">The image bytes.</param>
    /// <returns>A task whose <see cref="Body"/> is the base64 encoding of <paramref name="image"/>.</returns>
    public static ImageToTextTask FromBytes(byte[] image) => new(ImageBody.FromBytes(image));

    /// <inheritdoc />
    protected override void Validate(Validator v)
    {
        ImageBody.Validate(v, Body);
        if (MinLength is { } min)
        {
            v.Range(min, 0, int.MaxValue, nameof(MinLength));
        }
        if (MaxLength is { } max)
        {
            v.Range(max, 0, int.MaxValue, nameof(MaxLength));
        }
        if (MinLength > 0 && MaxLength > 0)
        {
            v.Must(MinLength <= MaxLength, nameof(MinLength), "must not be greater than MaxLength");
        }
        if (WebsiteUrl is not null)
        {
            v.Url(WebsiteUrl);
        }
    }
}
