using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;

namespace AntiCaptchaApi.Net.Requests
{
    /// <summary>
    /// Represents a request to solve Google Recaptcha V2 using your own proxy (RecaptchaV2Task).
    /// The task involves providing the target website URL, the Google site key, and your proxy details.
    /// The result is a g-recaptcha-response token.
    /// </summary>
    /// <remarks>
    /// This task type requires you to provide proxy information via the <see cref="ProxyConfig"/> property.
    /// If you do not want to use your own proxies, use <see cref="RecaptchaV2ProxylessRequest"/> instead.
    /// See https://anti-captcha.com/apidoc/task-types/RecaptchaV2Task for more details.
    /// Example captcha: https://anti-captcha.com/_nuxt/img/recaptcha-v2.db8dd45.png
    /// </remarks>
    public class RecaptchaV2Request : RecaptchaV2ProxylessRequest, IRecaptchaV2Request
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecaptchaV2Request"/> class.
        /// </summary>
        public RecaptchaV2Request()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecaptchaV2Request"/> class by copying properties from an existing request.
        /// </summary>
        /// <param name="request">The request object to copy properties from.</param>
        public RecaptchaV2Request(IRecaptchaV2Request request) : base(request)
        {
            ProxyConfig = request.ProxyConfig;
            UserAgent = request.UserAgent;
            Cookies = request.Cookies;
        }

        /// <summary>
        /// [Required] Gets or sets the proxy configuration details.
        /// All properties within <see cref="Models.ProxyConfig"/> (ProxyType, ProxyAddress, ProxyPort) are required for this task type.
        /// ProxyLogin and ProxyPassword are required if the proxy needs authentication.
        /// </summary>
        public ProxyConfig ProxyConfig { get; set; }

        /// <summary>
        /// [Required] Gets or sets the browser's User-Agent string to be used in emulation.
        /// Use a modern browser signature to avoid issues with Google.
        /// </summary>
        public string UserAgent { get; set; }
        
        /// <summary>
        /// [Optional] Gets or sets additional cookies to be used during the interaction with Google domains.
        /// Format: "key1=value1; key2=value2"
        /// </summary>
        public string Cookies { get; set; }
    }
}