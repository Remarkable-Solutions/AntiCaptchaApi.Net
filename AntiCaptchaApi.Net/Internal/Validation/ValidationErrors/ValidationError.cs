namespace AntiCaptchaApi.Net.Internal.Validation.ValidationErrors;

public class ValidationError(string propertyName, string errorMessage)
{
    public string PropertyName { get; } = propertyName;
    public string ErrorMessage { get; } = errorMessage;

    public override string ToString()
    {
        return $"{PropertyName} {ErrorMessage}";
    }
}