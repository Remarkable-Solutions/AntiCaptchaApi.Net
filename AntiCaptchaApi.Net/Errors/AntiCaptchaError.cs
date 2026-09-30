using AntiCaptchaApi.Net.Internal.Errors;

namespace AntiCaptchaApi.Net;

/// <summary>
/// Why an anti-captcha call or solve failed. It is carried by <see cref="AntiCaptchaException"/> and by failed
/// solve results. It never contains the client key or proxy passwords: descriptions are redacted before they get here.
/// </summary>
/// <param name="Code">The typed reason.</param>
/// <param name="RawCode">
/// The raw API <c>errorCode</c> (for example <c>ERROR_ZERO_BALANCE</c>); for <see cref="AntiCaptchaErrorCode.HttpError"/>
/// the HTTP status number; <see langword="null"/> when there is none.
/// </param>
/// <param name="Description">A human-readable explanation, or <see langword="null"/>.</param>
/// <param name="ErrorId">The API <c>errorId</c>, or <see langword="null"/> for client-side errors.</param>
public sealed record AntiCaptchaError(AntiCaptchaErrorCode Code, string? RawCode, string? Description, int? ErrorId)
{
    /// <summary>Whether a new attempt (a new task) may succeed, for example after <see cref="AntiCaptchaErrorCode.NoSlotAvailable"/>.</summary>
    public bool IsRetryable => ErrorClassifier.IsRetryable(this);

    /// <summary>Whether the failure is temporary transport trouble, so polling the same task can simply continue.</summary>
    public bool IsTransient => ErrorClassifier.IsTransient(this);
}
