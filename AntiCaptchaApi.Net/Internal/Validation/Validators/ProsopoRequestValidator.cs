using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Requests;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators;

/// <summary>
/// Validator for <see cref="ProsopoRequest"/>
/// </summary>
internal class ProsopoRequestValidator : ProsopoProxylessRequestValidator
{
    public ValidationResult Validate(ProsopoRequest request) =>
        base.Validate(request) // Calls WebsiteCaptchaRequestValidator.Validate(ProsopoProxylessRequest)
            .ValidateIsNotNullOrEmpty(nameof(request.UserAgent), request.UserAgent)
            .ValidateProxy(request.ProxyConfig); // Proxy is required for this task type
}