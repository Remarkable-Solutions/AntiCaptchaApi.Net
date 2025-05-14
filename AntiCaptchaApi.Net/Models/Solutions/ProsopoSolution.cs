using AntiCaptchaApi.Net.Models.Solutions; // Corrected namespace
using Newtonsoft.Json;

namespace AntiCaptchaApi.Net.Models.Solutions;

/// <summary>
/// Represents the solution for a Prosopo captcha task.
/// </summary>
public class ProsopoSolution : BaseSolution
{
    /// <summary>
    /// Token string required for interacting with the submit form on the target website.
    /// </summary>
    [JsonProperty(PropertyName = "token")]
    public string Token { get; set; }

    /// <summary>
    /// User-Agent of the worker's browser. Use it when you submit the response token.
    /// </summary>
    [JsonProperty(PropertyName = "userAgent")]
    public string UserAgent { get; set; }

    /// <inheritdoc/>
    public override bool IsValid()
    {
        return !string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(UserAgent);
    }
}