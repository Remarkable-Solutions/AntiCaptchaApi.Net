using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Internal.Validation.Validators.Base;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;

namespace AntiCaptchaApi.Net.Internal.Validation.Validators
{
    public class ImageToCoordinatesRequestValidator : CaptchaRequestValidator<ImageToCoordinatesRequest, ImageToCoordinatesSolution>
    {
        public override ValidationResult Validate(ImageToCoordinatesRequest request) =>
            new ValidationResult().ValidateIsNotNullOrEmpty(nameof(request.Body), request.Body);
    }
}