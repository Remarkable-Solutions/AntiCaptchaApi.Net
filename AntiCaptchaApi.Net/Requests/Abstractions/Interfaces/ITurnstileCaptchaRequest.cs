using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    public interface ITurnstileCaptchaRequest : ITurnstileCaptchaProxylessRequest, IProxyConfigWithUserAgentArgs
    {
    
    }
}