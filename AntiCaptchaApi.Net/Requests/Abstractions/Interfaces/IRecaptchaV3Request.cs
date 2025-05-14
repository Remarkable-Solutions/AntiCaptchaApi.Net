using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    public interface IRecaptchaV3Request : IWebCaptchaRequest<RecaptchaSolution>, IApiDomainArg, IIsEnterpriseArg
    {
        public string PageAction { get; set; }
        public decimal MinScore { get; set; }
    }
}