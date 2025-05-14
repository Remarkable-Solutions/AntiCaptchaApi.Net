using System.Collections.Generic;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args;
using Newtonsoft.Json.Linq;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    public interface IAntiGateRequest : ICaptchaRequest<AntiGateSolution>, IProxyConfigArg
    {
        public string TemplateName { get; set; }
        public JObject Variables { get; set; }
        public List<string> DomainsOfInterest  { get; set; }
    }
}