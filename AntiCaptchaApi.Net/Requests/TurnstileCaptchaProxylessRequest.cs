using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;

namespace AntiCaptchaApi.Net.Requests
{
    /// <summary>
    /// Solve Turnstile captcha without a proxy - TurnstileTaskProxyless
    ///
    /// Turnstile captcha is another attempt to replace Recaptcha.
    /// We support all its subtypes automatically: manual, non-interactive and invisible.
    /// No need to specify the subtype.
    /// Also providing your own custom User-Agent is not necessary and won't work at all.
    /// </summary>
    public class TurnstileCaptchaProxylessRequest : WebsiteCaptchaRequest<TurnstileSolution>, ITurnstileCaptchaProxylessRequest
    {
    
        public TurnstileCaptchaProxylessRequest()
        {
            
        }
        
        public TurnstileCaptchaProxylessRequest(ITurnstileCaptchaProxylessRequest request) : base(request)
        {
        }
    }
}