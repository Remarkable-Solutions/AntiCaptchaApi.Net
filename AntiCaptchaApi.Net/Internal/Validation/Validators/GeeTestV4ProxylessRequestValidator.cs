using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Internal.Validation.Validators.Base;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators
{
    public class GeeTestV4ProxylessRequestValidator : CaptchaRequestValidator<GeeTestV4ProxylessRequest, GeeTestV4Solution>
    {
        public override ValidationResult Validate(GeeTestV4ProxylessRequest request)
        {
            return base.Validate(request)
                .ValidateIsNotNullOrEmpty(nameof(request.WebsiteUrl), request.WebsiteUrl)
                .ValidateIsNotNullOrEmpty(nameof(request.Gt), request.Gt);
        }
    }
}