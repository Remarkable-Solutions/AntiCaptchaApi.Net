using System.Collections.Generic;
using AntiCaptchaApi.Net.Internal.Validation.ValidationErrors;

namespace AntiCaptchaApi.Net.Internal.Validation
{
    public class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
    }
}