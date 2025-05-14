using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    public interface IRecaptchaV2Request : IRecaptchaV2ProxylessRequest, IProxyConfigWithUserAgentArgs, ICookiesArg
    {
    
    }
}