using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Requests;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators
{
    /// <summary>
    /// Validator for <see cref="FriendlyCaptchaRequest"/>
    /// </summary>
    internal class FriendlyCaptchaRequestValidator : FriendlyCaptchaProxylessRequestValidator
    {
        public ValidationResult Validate(FriendlyCaptchaRequest request) =>
            base.Validate(request) // Calls WebsiteCaptchaRequestValidator.Validate(FriendlyCaptchaProxylessRequest)
                .ValidateIsNotNullOrEmpty(nameof(request.UserAgent), request.UserAgent)
                .ValidateProxy(request.ProxyConfig); // Proxy is required for this task type
    }
}