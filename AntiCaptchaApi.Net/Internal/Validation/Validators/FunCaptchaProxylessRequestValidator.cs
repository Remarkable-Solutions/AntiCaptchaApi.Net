using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Internal.Validation.Validators.Base;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators
{
    public class FunCaptchaProxylessRequestValidator : CaptchaRequestValidator<FunCaptchaProxylessRequest, FunCaptchaSolution>
    {
        public override ValidationResult Validate(FunCaptchaProxylessRequest request) =>
            base.Validate(request)
                .ValidateIsNotNullOrEmpty(nameof(request.WebsiteUrl), request.WebsiteUrl)
                .ValidateIsNotNullOrEmpty(nameof(request.WebsitePublicKey), request.WebsitePublicKey);
    }
}