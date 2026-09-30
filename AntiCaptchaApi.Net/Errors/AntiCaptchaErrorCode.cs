namespace AntiCaptchaApi.Net;

/// <summary>
/// The reason an anti-captcha call failed. API codes carry the documented <c>errorId</c> as their numeric value
/// (see https://anti-captcha.com/apidoc/errors); client-side codes start at 1000. A code this library does not know
/// is reported as <see cref="Unknown"/>, and the raw string stays in <see cref="AntiCaptchaError.RawCode"/>.
/// </summary>
public enum AntiCaptchaErrorCode
{
    /// <summary>The API returned an error code that this library does not know, or no code at all.</summary>
    Unknown = 0,

    /// <summary><c>ERROR_KEY_DOES_NOT_EXIST</c>: The account authorization key (client key) was not found.</summary>
    KeyDoesNotExist = 1,

    /// <summary><c>ERROR_NO_SLOT_AVAILABLE</c>: No idle workers are available at the moment. Retryable.</summary>
    NoSlotAvailable = 2,

    /// <summary><c>ERROR_ZERO_CAPTCHA_FILESIZE</c>: The uploaded captcha image is smaller than 100 bytes.</summary>
    ZeroCaptchaFileSize = 3,

    /// <summary><c>ERROR_TOO_BIG_CAPTCHA_FILESIZE</c>: The uploaded captcha image is larger than 500,000 bytes.</summary>
    TooBigCaptchaFileSize = 4,

    /// <summary><c>ERROR_ZERO_BALANCE</c>: The account has zero or negative balance.</summary>
    ZeroBalance = 10,

    /// <summary><c>ERROR_IP_NOT_ALLOWED</c>: Requests from this IP address are not allowed with this client key.</summary>
    IpNotAllowed = 11,

    /// <summary><c>ERROR_CAPTCHA_UNSOLVABLE</c>: Several workers could not solve the captcha. Retryable.</summary>
    CaptchaUnsolvable = 12,

    /// <summary><c>ERROR_BAD_DUPLICATES</c>: The 100% recognition feature did not get enough matching guesses.</summary>
    BadDuplicates = 13,

    /// <summary><c>ERROR_NO_SUCH_METHOD</c>: The API method does not exist.</summary>
    NoSuchMethod = 14,

    /// <summary><c>ERROR_IMAGE_TYPE_NOT_SUPPORTED</c>: The image type could not be determined or is not supported.</summary>
    ImageTypeNotSupported = 15,

    /// <summary><c>ERROR_NO_SUCH_CAPCHA_ID</c>: The task does not exist or has expired (raw code spelled <c>CAPCHA</c>).</summary>
    NoSuchCaptchaId = 16,

    /// <summary><c>ERROR_IP_BLOCKED</c>: The IP address is blocked because of improper API use.</summary>
    IpBlocked = 21,

    /// <summary><c>ERROR_TASK_ABSENT</c>: The <c>task</c> property is missing or empty in <c>createTask</c>.</summary>
    TaskAbsent = 22,

    /// <summary><c>ERROR_TASK_NOT_SUPPORTED</c>: The task type is not supported or is misspelled.</summary>
    TaskNotSupported = 23,

    /// <summary><c>ERROR_INCORRECT_SESSION_DATA</c>: Some of the required values for the task are missing.</summary>
    IncorrectSessionData = 24,

    /// <summary><c>ERROR_PROXY_CONNECT_REFUSED</c>: The worker could not connect to the task proxy: connection refused. Retryable.</summary>
    ProxyConnectionRefused = 25,

    /// <summary><c>ERROR_PROXY_CONNECT_TIMEOUT</c>: The worker could not connect to the task proxy: connection timed out.</summary>
    ProxyConnectionTimeout = 26,

    /// <summary><c>ERROR_PROXY_READ_TIMEOUT</c>: Reading from the task proxy timed out.</summary>
    ProxyReadTimeout = 27,

    /// <summary><c>ERROR_PROXY_BANNED</c>: The proxy IP address is banned by the target service.</summary>
    ProxyBanned = 28,

    /// <summary><c>ERROR_PROXY_TRANSPARENT</c>: The proxy is transparent and does not hide the worker IP address.</summary>
    ProxyTransparent = 29,

    /// <summary><c>ERROR_RECAPTCHA_TIMEOUT</c>: The reCAPTCHA task timed out, probably because of a slow proxy or captcha server.</summary>
    RecaptchaTimeout = 30,

    /// <summary><c>ERROR_RECAPTCHA_INVALID_SITEKEY</c>: The captcha provider reported that the site key is invalid.</summary>
    RecaptchaInvalidSiteKey = 31,

    /// <summary><c>ERROR_RECAPTCHA_INVALID_DOMAIN</c>: The captcha provider reported that the domain is invalid for this site key.</summary>
    RecaptchaInvalidDomain = 32,

    /// <summary><c>ERROR_RECAPTCHA_OLD_BROWSER</c>: The captcha provider requires a newer browser than the user agent sent.</summary>
    RecaptchaOldBrowser = 33,

    /// <summary><c>ERROR_TOKEN_EXPIRED</c>: The captcha provider reported that an additional token has expired.</summary>
    TokenExpired = 34,

    /// <summary><c>ERROR_PROXY_HAS_NO_IMAGE_SUPPORT</c>: The proxy does not transfer image data from the captcha servers.</summary>
    ProxyHasNoImageSupport = 35,

    /// <summary><c>ERROR_PROXY_INCOMPATIBLE_HTTP_VERSION</c>: The proxy does not support long GET requests or the required HTTP version.</summary>
    ProxyIncompatibleHttpVersion = 36,

    /// <summary><c>ERROR_PROXY_NOT_AUTHORISED</c>: The proxy login or password is incorrect.</summary>
    ProxyNotAuthorised = 49,

    /// <summary><c>ERROR_INVALID_KEY_TYPE</c>: The site key belongs to a different reCAPTCHA type (for example V3 instead of V2).</summary>
    InvalidKeyType = 51,

    /// <summary><c>ERROR_FAILED_LOADING_WIDGET</c>: The worker browser could not load the captcha widget.</summary>
    FailedLoadingWidget = 52,

    /// <summary><c>ERROR_VISIBLE_RECAPTCHA</c>: A visible reCAPTCHA was solved as invisible (or the other way round).</summary>
    VisibleRecaptcha = 53,

    /// <summary><c>ERROR_ALL_WORKERS_FILTERED</c>: No workers are left after filtering by <c>reportIncorrectRecaptcha</c>.</summary>
    AllWorkersFiltered = 54,

    /// <summary><c>ERROR_ACCOUNT_SUSPENDED</c>: The account is suspended.</summary>
    AccountSuspended = 55,

    /// <summary><c>ERROR_TEMPLATE_NOT_FOUND</c>: The AntiGate template name was not found.</summary>
    TemplateNotFound = 56,

    /// <summary><c>ERROR_TASK_CANCELED</c>: The AntiGate task was canceled by the worker.</summary>
    TaskCanceled = 57,

    // Client-side codes (no API errorId).
    /// <summary>The HTTP call failed: a network failure (no <see cref="AntiCaptchaError.RawCode"/>) or a non-2xx status (the status number is the raw code). Network failures, 408, 429 and 5xx are transient.</summary>
    HttpError = 1000,

    /// <summary>The HTTP call exceeded <c>AntiCaptchaClientOptions.RequestTimeout</c> or <c>HttpClient.Timeout</c> while the caller's token was not cancelled.</summary>
    HttpTimeout = 1001,

    /// <summary>The API response could not be parsed, or a ready task had no solution.</summary>
    InvalidResponse = 1002,

    /// <summary>The per-attempt <c>SolveOptions.Timeout</c> elapsed before the task was ready. Not retried, because the task may still be charged.</summary>
    SolveTimeout = 1003,

    /// <summary><c>SolveOptions.MaxCostPerSolve</c> was reached before a retry attempt.</summary>
    CostLimitReached = 1004,
}
