namespace AntiCaptchaApi.Net.Internal;

/// <summary>
/// The anti-captcha softId sent with every createTask call. It attributes solves to this library and is not overridable.
/// </summary>
internal static class SoftId
{
    internal const int Value = 1023;
}
