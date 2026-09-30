using System;

namespace AntiCaptchaApi.Net;

/// <summary>Client-wide settings of <see cref="AntiCaptchaClient"/>.</summary>
public sealed record AntiCaptchaClientOptions
{
    /// <summary>The API root. Each call is posted to <c>{BaseAddress}{method}</c>. Defaults to <c>https://api.anti-captcha.com/</c>.</summary>
    public Uri BaseAddress { get; init; } = new("https://api.anti-captcha.com/");

    /// <summary>
    /// The time limit of each HTTP call. When it elapses the call fails with <see cref="AntiCaptchaErrorCode.HttpTimeout"/>
    /// (a transient error). It must be positive. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
