using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    public interface IGeeTestV3ProxylessRequest : ICaptchaRequest<GeeTestV3Solution>, IGeeTestArgs
    {
    
    }
}