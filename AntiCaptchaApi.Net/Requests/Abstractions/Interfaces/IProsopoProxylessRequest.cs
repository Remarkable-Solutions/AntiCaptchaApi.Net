using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;

/// <summary>
/// Defines the contract for a ProsopoTaskProxyless request.
/// </summary>
public interface IProsopoProxylessRequest : ICaptchaRequest<ProsopoSolution>, IWebsiteUrlArg, IWebsiteKeyArg
{
    
}