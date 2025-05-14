using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators.Base
{
    public abstract class WebsiteCaptchaRequestValidator<TRequest, TSolution> : CaptchaRequestValidator<TRequest, TSolution>
        where TSolution : BaseSolution
        where TRequest : WebsiteCaptchaRequest<TSolution>
    {
        public override ValidationResult Validate(TRequest request) =>
            base.Validate(request)
                .ValidateIsNotNullOrEmpty(nameof(request.WebsiteKey), request.WebsiteKey)
                .ValidateIsNotNullOrEmpty(nameof(request.WebsiteUrl), request.WebsiteUrl);
    }
}