using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args;

using AntiCaptchaApi.Net.Models.Solutions;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    public interface IAmazonWafProxylessRequest : IWebCaptchaRequest<AmazonWafSolution>
    {
        /// <summary>
        /// Value of iv from window.gokuProps object in WAF page source code.
        /// </summary>
        string Iv { get; set; }
        /// <summary>
        /// Value of context from window.gokuProps object in WAF page source code.
        /// </summary>
        string Context { get; set; }
        /// <summary>
        /// Optional URL leading to captcha.js
        /// </summary>
        string CaptchaScript { get; set; }
        /// <summary>
        /// Optional URL leading to challenge.js
        /// </summary>
        string ChallengeScript { get; set; }
    }
}