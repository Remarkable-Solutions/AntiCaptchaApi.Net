using AntiCaptchaApi.Net.Internal.Validation.Validators.Base;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests; 
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators;

/// <summary>
/// Validator for <see cref="IFriendlyCaptchaProxylessRequest"/>
/// </summary>
internal class FriendlyCaptchaProxylessRequestValidator : WebsiteCaptchaRequestValidator<FriendlyCaptchaProxylessRequest, ProsopoSolution>
{
    // The base class WebsiteCaptchaRequestValidator already handles WebsiteUrl and WebsiteKey validation.
    // No additional validation logic is needed here based on the provided documentation for the proxyless version.
}