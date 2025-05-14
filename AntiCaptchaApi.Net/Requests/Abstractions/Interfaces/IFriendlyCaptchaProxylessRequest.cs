using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    /// <summary>
    /// Defines the contract for a FriendlyCaptchaTaskProxyless request.
    /// </summary>
    public interface IFriendlyCaptchaProxylessRequest : ICaptchaRequest<ProsopoSolution>, IWebsiteUrlArg, IWebsiteKeyArg
    {
    
    }
}