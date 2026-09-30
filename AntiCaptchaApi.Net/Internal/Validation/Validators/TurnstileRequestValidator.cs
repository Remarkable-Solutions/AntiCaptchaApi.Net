using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Internal.Validation.Validators.Base;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators
{
    public class TurnstileRequestValidator : TurnstileProxylessRequestValidator
    {
        // TurnstileTask requires a proxy; a custom UserAgent is not needed for Turnstile, so it stays optional.
        public override ValidationResult Validate(TurnstileCaptchaProxylessRequest request)
        {
            return base.Validate(request)
                .ValidateProxy((request as TurnstileCaptchaRequest)?.ProxyConfig);
        }
    }
}