using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;
using Newtonsoft.Json;

namespace AntiCaptchaApi.Net.Requests
{
    /// <summary>
    /// Bypass Amazon WAF
    /// Use this type of tasks to obtain Amazon WAF cookie token. Simply grab temporary iv and context tokens along with permanent key site key, send them to our API. Result of the task is a token which you can use in your HTTP request as a cookie value with name amazon-waf-token.
    /// This task type uses your own proxy.
    /// </summary>
    public class AmazonWafRequest : WebsiteCaptchaRequest<AmazonWafSolution>, IAmazonWafRequest
    {
        /// <summary>
        /// Value of iv from window.gokuProps object in WAF page source code.
        /// </summary>
        [JsonProperty("iv")]
        public string Iv { get; set; }
        
        /// <summary>
        /// Value of context from window.gokuProps object in WAF page source code.
        /// </summary>
        [JsonProperty("context")]
        public string Context { get; set; }
        
        /// <summary>
        /// Optional URL leading to captcha.js
        /// </summary>
        [JsonProperty("captchaScript")]
        public string CaptchaScript { get; set; }
        
        /// <summary>
        /// Optional URL leading to challenge.js
        /// </summary>
        [JsonProperty("challengeScript")]
        public string ChallengeScript { get; set; }

        /// <summary>
        /// Proxy configuration.
        /// </summary>
        [JsonProperty("proxyConfig")]
        public ProxyConfig ProxyConfig { get; set; }

        public AmazonWafRequest()
        {
            
        }

        public AmazonWafRequest(IAmazonWafRequest request) : base(request)
        {
            Iv = request.Iv;
            Context = request.Context;
            CaptchaScript = request.CaptchaScript;
            ChallengeScript = request.ChallengeScript;
            ProxyConfig = request.ProxyConfig;
        }
    }
}