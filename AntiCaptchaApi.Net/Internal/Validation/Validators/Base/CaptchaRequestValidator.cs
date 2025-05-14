using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators.Base
{
    public abstract class CaptchaRequestValidator<TRequest, TSolution> 
        where TRequest : CaptchaRequest<TSolution> where TSolution : BaseSolution
    {
        public virtual ValidationResult Validate(TRequest request) => new ValidationResult();
    }
}