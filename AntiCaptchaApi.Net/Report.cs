namespace AntiCaptchaApi.Net;

/// <summary>
/// A report on a solved task for <see cref="IAntiCaptchaClient.ReportAsync"/>. Reports must be sent within 60 seconds of
/// completion, and only one per task. 2.x <c>ReportOutcome.IncorrectHCaptcha</c> has no equivalent (hCaptcha is not supported).
/// </summary>
public enum Report
{
    /// <summary>
    /// An image captcha (English ImageToText) was solved incorrectly; a confirmed complaint is refunded
    /// (<see href="https://anti-captcha.com/apidoc/methods/reportIncorrectImageCaptcha"/>).
    /// </summary>
    IncorrectImageCaptcha = 0,

    /// <summary>
    /// A reCAPTCHA v2/v3 (including Enterprise) token was rejected; removes the worker from your whitelist
    /// (<see href="https://anti-captcha.com/apidoc/methods/reportIncorrectRecaptcha"/>).
    /// </summary>
    IncorrectRecaptcha = 1,

    /// <summary>
    /// A reCAPTCHA v3 or v2 Enterprise token was accepted; adds the worker to your whitelist
    /// (<see href="https://anti-captcha.com/apidoc/methods/reportCorrectRecaptcha"/>).
    /// </summary>
    CorrectRecaptcha = 2,
}
