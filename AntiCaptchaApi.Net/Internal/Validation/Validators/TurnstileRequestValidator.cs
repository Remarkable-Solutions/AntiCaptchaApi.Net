using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Internal.Validation.Validators.Base;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators
{
    public class TurnstileRequestValidator : TurnstileProxylessRequestValidator
    {
        public override ValidationResult Validate(TurnstileCaptchaProxylessRequest request)
        {
            return base.Validate(request)
                .ValidateProxy(((TurnstileCaptchaRequest)request).ProxyConfig)
                .ValidateIsNotNullOrEmpty(nameof(TurnstileCaptchaRequest.UserAgent), ((TurnstileCaptchaRequest)request).UserAgent);
        }
    }
}