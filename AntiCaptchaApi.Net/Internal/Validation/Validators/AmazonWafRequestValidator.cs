using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Internal.Validation.Validators.Base;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators
{
    public class AmazonWafRequestValidator : WebsiteCaptchaRequestValidator<AmazonWafRequest, AmazonWafSolution>
    {
        public override ValidationResult Validate(AmazonWafRequest request) =>
            base.Validate(request)
                .ValidateOptionalProxy(request.ProxyConfig);
    }
}