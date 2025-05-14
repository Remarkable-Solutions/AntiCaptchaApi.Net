using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;

namespace AntiCaptchaApi.Net.Requests
{
    /// <summary>
    /// Solve Turnstile captcha via a proxy - TurnstileTask
    ///
    /// Turnstile captcha is another attempt to replace Recaptcha.
    /// We support all its subtypes automatically: manual, non-interactive and invisible.
    /// No need to specify the subtype.
    /// Also providing your own custom User-Agent is not necessary and won't work at all.
    ///
    /// This type of task requires a proxy. Please use it only if proxy-off tasks (TurnstileTaskProxyless) are failing, as it slows down our workers.
    /// Solving captchas with proxies also requires super high quality of your proxies which you should install yourself on your own VPS servers and never use purchased proxy services.
    /// </summary>
    public class TurnstileCaptchaRequest : TurnstileCaptchaProxylessRequest, ITurnstileCaptchaRequest
    {
        /// <summary>
        /// [Required] ProxyConfig.ProxyType : Type of proxy http http/socks4/socks4.
        /// [Required] ProxyConfig.proxyAddress : Proxy IP address ipv4/ipv6. No host names or IP addresses from local networks.
        /// [Required] ProxyConfig.proxyPort : Proxy port.
        /// [Optional] ProxyConfig.proxyLogin : Login for proxy which requires authorization (basic)
        /// [Optional] ProxyConfig.proxyPassword : Proxy password
        /// </summary>
        /// 
        public ProxyConfig ProxyConfig { get; set; }
    
        /// <summary>
        /// [Required]
        /// Browser's User-Agent used in emulation.
        /// You must use a modern-browser signature; otherwise, Google will ask you to "update your browser".
        /// </summary>
        /// 
        public string UserAgent { get; set; }

        public TurnstileCaptchaRequest()
        {
            
        }
        
        public TurnstileCaptchaRequest(ITurnstileCaptchaRequest request) : base(request)
        {
            ProxyConfig = request.ProxyConfig;
            UserAgent = request.UserAgent;
        }
    }
}