using System;
using System.IO;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>Shared loading and validation of the base64 <c>body</c> of image tasks.</summary>
internal static class ImageBody
{
    private const string DataUriPrefix = "data:";

    public static string FromFile(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The image file was not found.", path);
        }
        return Convert.ToBase64String(File.ReadAllBytes(path));
    }

    public static string FromBytes(byte[] bytes) =>
        Convert.ToBase64String(bytes ?? throw new ArgumentNullException(nameof(bytes)));

    public static void Validate(Validator v, string? body)
    {
        v.NotEmpty(body, "Body");
        if (body is not null)
        {
            v.Must(!body.TrimStart().StartsWith(DataUriPrefix, StringComparison.OrdinalIgnoreCase), "Body",
                "must be raw base64 without a data: URI prefix");
        }
    }
}
