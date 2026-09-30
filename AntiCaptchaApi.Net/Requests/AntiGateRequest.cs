using System.Collections.Generic;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AntiCaptchaApi.Net.Requests
{
    /// <summary>
    /// AntiGate task: a worker opens <see cref="CaptchaRequest{TSolution}.WebsiteUrl"/> in a real browser and runs
    /// the AntiGate template <see cref="TemplateName"/> with the given <see cref="Variables"/>. The result contains
    /// cookies, local storage, the browser fingerprint and more (see <see cref="AntiGateSolution"/>).
    /// The task type is always <c>AntiGateTask</c>, with or without a proxy.
    /// Documentation: https://anti-captcha.com/apidoc/task-types/AntiGateTask
    /// </summary>
    public class AntiGateRequest : CaptchaRequest<AntiGateSolution>, IAntiGateRequest
    {
        /// <summary>
        /// [Required]
        /// Name of a scenario template from the AntiGate catalog (or your own private template).
        /// </summary>
        [JsonProperty("templateName")]
        public string TemplateName { get; set; }

        /// <summary>
        /// [Required]
        /// Template variables as a JSON object, e.g. <c>JObject.FromObject(new { css_selector = "#x" })</c>.
        /// Keys are sent unchanged.
        /// </summary>
        [JsonProperty("variables")]
        public JObject Variables { get; set; }

        /// <summary>
        /// [Optional]
        /// Domains whose cookies and local storage should be collected in addition to the domain of the final page.
        /// </summary>
        [JsonProperty("domainsOfInterest")]
        public List<string> DomainsOfInterest { get; set; }

        /// <summary>
        /// [Optional]
        /// Proxy for the worker's browser. When set, address and port are required; anti-captcha accepts only
        /// HTTPS proxies given by IP address for this task type. <see cref="ProxyConfig.ProxyType"/> is not sent.
        /// </summary>
        [JsonProperty("proxyConfig")]
        public ProxyConfig ProxyConfig { get; set; }

        /// <summary>
        /// Creates an empty AntiGate request.
        /// </summary>
        public AntiGateRequest()
        {
        }

        /// <summary>
        /// Creates an AntiGate request copying all values from <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The request to copy.</param>
        public AntiGateRequest(IAntiGateRequest request) : base(request)
        {
            TemplateName = request.TemplateName;
            Variables = request.Variables;
            DomainsOfInterest = request.DomainsOfInterest;
            ProxyConfig = request.ProxyConfig;
        }
    }
}
