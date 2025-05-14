using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;
using Newtonsoft.Json;

namespace AntiCaptchaApi.Net.Requests
{
    /// <summary>
    /// Represents a request for solving Prosopo captcha without proxies.
    /// </summary>
    public class ProsopoProxylessRequest : WebsiteCaptchaRequest<ProsopoSolution>, IProsopoProxylessRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProsopoProxylessRequest"/> class.
        /// </summary>
        public ProsopoProxylessRequest()
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProsopoProxylessRequest"/> class.
        /// </summary>
        /// <param name="websiteUrl">Address of a target web page.</param>
        /// <param name="websiteKey">Prosopo sitekey.</param>
        public ProsopoProxylessRequest(string websiteUrl, string websiteKey)
        {
            WebsiteUrl = websiteUrl;
            WebsiteKey = websiteKey;
        }
    }
}