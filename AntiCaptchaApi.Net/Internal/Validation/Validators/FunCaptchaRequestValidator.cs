using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Requests;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators
{
    public class FunCaptchaRequestValidator : FunCaptchaProxylessRequestValidator
    {
        public override ValidationResult Validate(FunCaptchaProxylessRequest request) =>
            base.Validate(request)
                .ValidateIsNotNullOrEmpty(nameof(FunCaptchaRequest.UserAgent), ((FunCaptchaRequest)request).UserAgent)
                .ValidateOptionalProxy(((FunCaptchaRequest)request).ProxyConfig);
    }
}