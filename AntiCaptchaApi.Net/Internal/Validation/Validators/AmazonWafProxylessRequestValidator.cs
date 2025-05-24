using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Internal.Validation.Validators.Base;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators
{
    public class AmazonWafProxylessRequestValidator : WebsiteCaptchaRequestValidator<AmazonWafProxylessRequest, AmazonWafSolution>
    {
        public override ValidationResult Validate(AmazonWafProxylessRequest request) =>
            base.Validate(request)
                .ValidateIsNotNullOrEmpty(nameof(request.Iv), request.Iv)
                .ValidateIsNotNullOrEmpty(nameof(request.Context), request.Context);
    }
}