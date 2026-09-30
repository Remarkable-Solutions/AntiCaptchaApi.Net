using System.Collections.Generic;

namespace AntiCaptchaApi.Net.Internal.Errors;

/// <summary>Maps the raw <c>errorCode</c> strings of the API to <see cref="AntiCaptchaErrorCode"/> (ordinal, case-sensitive).</summary>
internal static class ErrorCodeParser
{
    private static readonly Dictionary<string, AntiCaptchaErrorCode> Codes = new(System.StringComparer.Ordinal)
    {
        ["ERROR_KEY_DOES_NOT_EXIST"] = AntiCaptchaErrorCode.KeyDoesNotExist,
        ["ERROR_NO_SLOT_AVAILABLE"] = AntiCaptchaErrorCode.NoSlotAvailable,
        ["ERROR_ZERO_CAPTCHA_FILESIZE"] = AntiCaptchaErrorCode.ZeroCaptchaFileSize,
        ["ERROR_TOO_BIG_CAPTCHA_FILESIZE"] = AntiCaptchaErrorCode.TooBigCaptchaFileSize,
        ["ERROR_ZERO_BALANCE"] = AntiCaptchaErrorCode.ZeroBalance,
        ["ERROR_IP_NOT_ALLOWED"] = AntiCaptchaErrorCode.IpNotAllowed,
        ["ERROR_CAPTCHA_UNSOLVABLE"] = AntiCaptchaErrorCode.CaptchaUnsolvable,
        ["ERROR_BAD_DUPLICATES"] = AntiCaptchaErrorCode.BadDuplicates,
        ["ERROR_NO_SUCH_METHOD"] = AntiCaptchaErrorCode.NoSuchMethod,
        ["ERROR_IMAGE_TYPE_NOT_SUPPORTED"] = AntiCaptchaErrorCode.ImageTypeNotSupported,
        ["ERROR_NO_SUCH_CAPCHA_ID"] = AntiCaptchaErrorCode.NoSuchCaptchaId,
        ["ERROR_IP_BLOCKED"] = AntiCaptchaErrorCode.IpBlocked,
        ["ERROR_TASK_ABSENT"] = AntiCaptchaErrorCode.TaskAbsent,
        ["ERROR_TASK_NOT_SUPPORTED"] = AntiCaptchaErrorCode.TaskNotSupported,
        ["ERROR_INCORRECT_SESSION_DATA"] = AntiCaptchaErrorCode.IncorrectSessionData,
        ["ERROR_PROXY_CONNECT_REFUSED"] = AntiCaptchaErrorCode.ProxyConnectionRefused,
        ["ERROR_PROXY_CONNECT_TIMEOUT"] = AntiCaptchaErrorCode.ProxyConnectionTimeout,
        ["ERROR_PROXY_READ_TIMEOUT"] = AntiCaptchaErrorCode.ProxyReadTimeout,
        ["ERROR_PROXY_BANNED"] = AntiCaptchaErrorCode.ProxyBanned,
        ["ERROR_PROXY_TRANSPARENT"] = AntiCaptchaErrorCode.ProxyTransparent,
        ["ERROR_RECAPTCHA_TIMEOUT"] = AntiCaptchaErrorCode.RecaptchaTimeout,
        ["ERROR_RECAPTCHA_INVALID_SITEKEY"] = AntiCaptchaErrorCode.RecaptchaInvalidSiteKey,
        ["ERROR_RECAPTCHA_INVALID_DOMAIN"] = AntiCaptchaErrorCode.RecaptchaInvalidDomain,
        ["ERROR_RECAPTCHA_OLD_BROWSER"] = AntiCaptchaErrorCode.RecaptchaOldBrowser,
        ["ERROR_TOKEN_EXPIRED"] = AntiCaptchaErrorCode.TokenExpired,
        ["ERROR_PROXY_HAS_NO_IMAGE_SUPPORT"] = AntiCaptchaErrorCode.ProxyHasNoImageSupport,
        ["ERROR_PROXY_INCOMPATIBLE_HTTP_VERSION"] = AntiCaptchaErrorCode.ProxyIncompatibleHttpVersion,
        ["ERROR_PROXY_NOT_AUTHORISED"] = AntiCaptchaErrorCode.ProxyNotAuthorised,
        ["ERROR_INVALID_KEY_TYPE"] = AntiCaptchaErrorCode.InvalidKeyType,
        ["ERROR_FAILED_LOADING_WIDGET"] = AntiCaptchaErrorCode.FailedLoadingWidget,
        ["ERROR_VISIBLE_RECAPTCHA"] = AntiCaptchaErrorCode.VisibleRecaptcha,
        ["ERROR_ALL_WORKERS_FILTERED"] = AntiCaptchaErrorCode.AllWorkersFiltered,
        ["ERROR_ACCOUNT_SUSPENDED"] = AntiCaptchaErrorCode.AccountSuspended,
        ["ERROR_TEMPLATE_NOT_FOUND"] = AntiCaptchaErrorCode.TemplateNotFound,
        ["ERROR_TASK_CANCELED"] = AntiCaptchaErrorCode.TaskCanceled,
    };

    public static AntiCaptchaErrorCode Parse(string? rawCode) =>
        rawCode is not null && Codes.TryGetValue(rawCode, out var code) ? code : AntiCaptchaErrorCode.Unknown;

    /// <summary>Builds the error of an API response with a non-zero <c>errorId</c>. The raw code is always kept.</summary>
    public static AntiCaptchaError FromApi(int errorId, string? rawCode, string? description) =>
        new(Parse(rawCode), rawCode, description, errorId);
}
