namespace AntiCaptchaApi.Net.Tasks;

/// <summary>The reCAPTCHA API domains that anti-captcha accepts in <c>apiDomain</c>.</summary>
internal static class RecaptchaApiDomain
{
    public static readonly string[] Allowed = { "www.google.com", "www.recaptcha.net" };

    public static void Validate(Validator v, string? apiDomain)
    {
        if (apiDomain is not null)
        {
            v.OneOf(apiDomain, Allowed, "ApiDomain");
        }
    }
}
