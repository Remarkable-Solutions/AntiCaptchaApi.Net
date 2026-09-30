using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Internal.Validation.Validators.Base;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators
{
    public class RecaptchaV3RequestValidator : WebsiteCaptchaRequestValidator<RecaptchaV3Request, RecaptchaSolution>
    {
        // 0.3, 0.7 and 0.9 are anti-captcha's worker queues; 0.5 is kept for 2.x backward compatibility.
        // IsEnterprise is optional: null means false (and RecaptchaV3EnterpriseRequest forces true in Build()).
        private static readonly decimal[] AcceptedMinScores = { 0.3m, 0.5m, 0.7m, 0.9m };

        public override ValidationResult Validate(RecaptchaV3Request request)
        {
            return base.Validate(request)
                .ValidateIsOneOfTheValues(nameof(request.MinScore), request.MinScore, AcceptedMinScores);
        }
    }
}