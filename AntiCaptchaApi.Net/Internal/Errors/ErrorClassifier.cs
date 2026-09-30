using System.Globalization;

namespace AntiCaptchaApi.Net.Internal.Errors;

/// <summary>
/// Retry classification of spec FR-6. <em>Retryable</em> errors justify a new attempt (a new task); <em>transient</em>
/// errors justify polling the same task again. Everything else is fatal.
/// </summary>
internal static class ErrorClassifier
{
    /// <summary>
    /// Builds an <see cref="AntiCaptchaErrorCode.HttpError"/>. The status number is kept as the raw code, so that the
    /// classification (and the caller) can tell a network failure (<paramref name="status"/> null) from a status error.
    /// </summary>
    public static AntiCaptchaError HttpError(int? status, string description) =>
        new(AntiCaptchaErrorCode.HttpError, status?.ToString(CultureInfo.InvariantCulture), description, null);

    public static bool IsRetryable(AntiCaptchaError error) => error.Code switch
    {
        AntiCaptchaErrorCode.NoSlotAvailable or AntiCaptchaErrorCode.CaptchaUnsolvable or AntiCaptchaErrorCode.ProxyConnectionRefused => true,
        AntiCaptchaErrorCode.HttpError or AntiCaptchaErrorCode.HttpTimeout => IsTransient(error),
        _ => false,
    };

    public static bool IsTransient(AntiCaptchaError error) => error.Code switch
    {
        AntiCaptchaErrorCode.HttpError => IsTransientStatus(error.RawCode),
        AntiCaptchaErrorCode.HttpTimeout or AntiCaptchaErrorCode.InvalidResponse => true,
        _ => false,
    };

    // No status means the request never got an HTTP answer (a network failure), which is transient.
    private static bool IsTransientStatus(string? rawStatus)
    {
        if (rawStatus is null)
        {
            return true;
        }
        if (!int.TryParse(rawStatus, NumberStyles.None, CultureInfo.InvariantCulture, out var status))
        {
            return false;
        }
        return status is 408 or 429 or >= 500;
    }
}
