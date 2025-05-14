using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    public interface IRecaptchaV2EnterpriseProxylessRequest : IEnterprisePayloadArg, IWebCaptchaRequest<RecaptchaSolution>, IApiDomainArg
    {
    
    }
}