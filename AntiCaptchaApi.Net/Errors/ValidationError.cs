namespace AntiCaptchaApi.Net;

/// <summary>One failed validation rule of a task, reported through <see cref="AntiCaptchaValidationException"/>.</summary>
/// <param name="Field">The name of the invalid property, for example <c>WebsiteUrl</c> or <c>Proxy.Port</c>.</param>
/// <param name="Message">What is wrong with the value. It never contains the value itself.</param>
public sealed record ValidationError(string Field, string Message);
