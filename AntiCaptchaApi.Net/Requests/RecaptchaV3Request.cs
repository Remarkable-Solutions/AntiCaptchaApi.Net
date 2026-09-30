using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;

namespace AntiCaptchaApi.Net.Requests
{
    /// <summary>
    /// Bypass Recaptcha V3
    ///
    /// This type of task object is required to solve Google Recaptcha V3 on a worker's computer.
    /// This task is executed by our service using our own proxy servers and/or workers' IP addresses.
    ///
    /// Please note that there's a difference between Recaptcha V2-invisible and Recaptcha V3.
    /// They look the same, and it might be confusing.
    /// There's a quick way to determine the correct type: try solving it with our API as V2-invisible and V3.
    /// In one of your attempts, you'll get an error, and in the other you won't.
    ///
    /// We test our workers for their recaptcha "score" and put them in 3 groups (queues): 0.3, 0.7 and 0.9.
    /// Each might have slightly different pricing due to the number of idle workers.
    /// By passing property minScore you define which queue your task goes into.
    /// Most of our available workers have score 0.3, and the minority have 0.9.More information about Recaptcha V3 can be found in <see href="https://anti-captcha.com/faq/449_everything_about_recaptcha_v3">this</see> FAQ section.
    /// 
    /// /// Example captcha: https://anti-captcha.com/_nuxt/img/recaptchav3.1b1650f.jpg
    /// </summary>
    public class RecaptchaV3Request : WebsiteCaptchaRequest<RecaptchaSolution>, IRecaptchaV3Request
    {
        /// <summary>
        /// [Required]
        /// Filters workers with a particular score. Use 0.3, 0.7 or 0.9 (anti-captcha's worker queues).
        /// 0.5 is accepted for backward compatibility but is not an anti-captcha queue and is discouraged.
        /// Default: 0.3.
        /// </summary>
        public decimal MinScore { get; set; } = 0.3m;

        /// <summary>
        /// [Optional]
        /// Recaptcha's "action" value. Website owners use this parameter to define what users are doing on the page.
        /// Example:
        ///     grecaptcha.execute('site_key', {action:'login_test'})
        /// </summary>
        public string PageAction { get; set; }
        
        /// <summary>
        /// [Optional]
        /// Set this flag to "true" if you need this V3 solved with Enterprise API.
        /// null = false: the value is not sent and Recaptcha is solved with the non-enterprise API.
        /// Can be determined by a javascript call like in the following example:
        ///
        /// grecaptcha.enterprise.execute('site_key', {..})
        /// </summary>
        public bool? IsEnterprise { get; set; }
        
        /// <summary>
        /// [Optional]
        /// Use this parameter to send the domain name from which the Recaptcha script should be served.
        /// Can have only one of two values: "www.google.com" or "www.recaptcha.net".
        /// Do not use this parameter unless you understand what you are doing.
        /// </summary>
        public string ApiDomain { get; set; }
        
        

        public RecaptchaV3Request()
        {
            
        }
        
        public RecaptchaV3Request(IRecaptchaV3Request request) : base(request)
        {
            ApiDomain = request.ApiDomain;
            IsEnterprise = request.IsEnterprise;
            PageAction = request.PageAction;
            MinScore = request.MinScore;
        }
    }
}