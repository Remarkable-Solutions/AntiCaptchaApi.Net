namespace AntiCaptchaApi.Net;

/// <summary>
/// A worker queue for <see cref="IAntiCaptchaClient.GetQueueStatsAsync"/>. The values are the documented
/// <c>queueId</c> numbers (<see href="https://anti-captcha.com/apidoc/methods/getQueueStats"/>).
/// </summary>
public enum QueueType
{
    /// <summary>ImageToText, English (queue 1).</summary>
    ImageToTextEng = 1,

    /// <summary>ImageToText, Russian (queue 2).</summary>
    ImageToTextRus = 2,

    /// <summary>reCAPTCHA v2 with a proxy (queue 5). Named <c>RecaptchaB2WithProxy</c> in 2.x.</summary>
    RecaptchaV2WithProxy = 5,

    /// <summary>reCAPTCHA v2 without a proxy (queue 6). Named <c>RecaptchaB2WithoutProxy</c> in 2.x.</summary>
    RecaptchaV2WithoutProxy = 6,

    /// <summary>FunCaptcha with a proxy (queue 7).</summary>
    FuncaptchaWithProxy = 7,

    /// <summary>FunCaptcha without a proxy (queue 10).</summary>
    FuncaptchaWithoutProxy = 10,

    /// <summary>GeeTest with a proxy (queue 12).</summary>
    GeeTestWithProxy = 12,

    /// <summary>GeeTest without a proxy (queue 13).</summary>
    GeeTestWithoutProxy = 13,

    /// <summary>reCAPTCHA v3, minimum score 0.3 (queue 18).</summary>
    RecaptchaV3s03 = 18,

    /// <summary>reCAPTCHA v3, minimum score 0.7 (queue 19).</summary>
    RecaptchaV3s07 = 19,

    /// <summary>reCAPTCHA v3, minimum score 0.9 (queue 20).</summary>
    RecaptchaV3s09 = 20,

    /// <summary>reCAPTCHA v2 Enterprise with a proxy (queue 23).</summary>
    RecaptchaEnterpriseV2WithProxy = 23,

    /// <summary>reCAPTCHA v2 Enterprise without a proxy (queue 24).</summary>
    RecaptchaEnterpriseV2WithoutProxy = 24,

    /// <summary>AntiGate templates (queue 25).</summary>
    AntiGateTask = 25,

    /// <summary>Cloudflare Turnstile with a proxy (queue 26). New in 3.0.</summary>
    TurnstileWithProxy = 26,

    /// <summary>Cloudflare Turnstile without a proxy (queue 27). New in 3.0.</summary>
    TurnstileWithoutProxy = 27,

    /// <summary>ImageToCoordinates (queue 28). New in 3.0.</summary>
    ImageToCoordinates = 28,
}
