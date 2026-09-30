using System;

namespace AntiCaptchaApi.Net;

/// <summary>Optional <c>createTask</c> settings. <c>SolveOptions</c> derives from it, so a solve can pass them too.</summary>
public record CreateOptions
{
    /// <summary>
    /// The worker pool: <c>en</c> (default, English-speaking workers) or <c>rn</c> (Russian-speaking workers), as
    /// documented by anti-captcha. <see langword="null"/> leaves it to the API.
    /// </summary>
    public string? LanguagePool { get; init; }

    /// <summary>An absolute URL that anti-captcha posts the result to when the task is done, or <see langword="null"/>.</summary>
    public Uri? CallbackUrl { get; init; }
}
