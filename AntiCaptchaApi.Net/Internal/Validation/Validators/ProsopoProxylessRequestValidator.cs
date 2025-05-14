using AntiCaptchaApi.Net.Internal.Validation.Validators.Base;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests; // Added for ProsopoProxylessRequest
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces; // For the interface in the summary

namespace AntiCaptchaApi.Net.Internal.Validation.Validators;

/// <summary>
/// Validator for <see cref="IProsopoProxylessRequest"/>
/// </summary>
internal class ProsopoProxylessRequestValidator : WebsiteCaptchaRequestValidator<ProsopoProxylessRequest, ProsopoSolution>
{
    // The base class WebsiteCaptchaRequestValidator already handles WebsiteUrl and WebsiteKey validation.
    // No additional validation logic is needed here based on the provided documentation.
}