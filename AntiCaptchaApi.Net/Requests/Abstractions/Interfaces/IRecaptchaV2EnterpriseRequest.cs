using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    public interface IRecaptchaV2EnterpriseRequest : IRecaptchaV2EnterpriseProxylessRequest, IProxyConfigWithUserAgentArgs, ICookiesArg
    {
    
    }
}