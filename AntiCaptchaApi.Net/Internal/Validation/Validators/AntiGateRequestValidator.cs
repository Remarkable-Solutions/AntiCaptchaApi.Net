using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Internal.Validation.Validators.Base;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators
{
    internal class AntiGateRequestValidator : CaptchaRequestValidator<AntiGateRequest, AntiGateSolution>
    {
        // The proxy is optional as a whole for AntiGateTask; when one is given it must be complete.
        // ValidateOptionalProxy is not used because it reports an error for a null proxy.
        public override ValidationResult Validate(AntiGateRequest request)
        {
            var result = base.Validate(request)
                .ValidateIsNotNullOrEmpty(nameof(request.WebsiteUrl), request.WebsiteUrl)
                .ValidateIsNotNullOrEmpty(nameof(request.TemplateName), request.TemplateName)
                .ValidateIsNotNull(nameof(request.Variables), request.Variables);

            return request.ProxyConfig == null ? result : result.ValidateProxy(request.ProxyConfig);
        }
    }
}
